using System.Collections.Generic;
using System.Linq;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Mapping;
using VirtoCommerce.XCart.Data.Services;
using AutoMapper;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Local fixture builders for the CART STATE mutation cluster:
/// <c>changeCartCurrency</c>, <c>mergeCart</c>, <c>clearCart</c>, <c>refreshCart</c>,
/// <c>changePurchaseOrderNumber</c>, <c>changeComment</c>, <c>createCart</c>.
///
/// Design rule: everything I/O is mocked at the leaf; pure compute (totals calculator) runs for real.
/// All factory methods return handlers wired to the shared <see cref="CartBenchmarkFixtures"/>
/// harness primitives where possible; local extensions handle merge's two-cart repo and
/// currency-change's same-currency shortcut.
/// </summary>
internal static class CartStateBenchmarkFixtures
{
    // ── changeCartCurrency ───────────────────────────────────────────────────────────────────────
    // CURRENCY GOTCHA: The handler loads the CURRENT cart (benchmark-cart) then tries to load OR
    // CREATE a new-currency cart using NewCurrencyCode. The shared currency service mock returns
    // only USD. If NewCurrencyCode != "USD", InnerGetCartAggregateFromCartAsync finds no currency
    // and throws. Fix: set NewCurrencyCode = "USD" — same-currency switch, which still exercises
    // the full CopyItems path (clear + add items) plus two RecalculateAsync calls. The benchmark
    // measures realistic compute on the success path; the currency-name identity is immaterial.

    /// <summary>
    /// Builds the real <see cref="ChangeCartCurrencyCommandHandler"/> over a LOCAL repository harness.
    /// <para>
    /// A local harness is needed because <see cref="CartBenchmarkFixtures.CreateMutationHarness"/>
    /// uses a loose mock for <c>IShoppingCartSearchService</c> (returns null from SearchAllAsync).
    /// The currency handler calls <c>GetOrCreateCartFromCommandAsync</c> TWICE: the first call uses
    /// CartId → <c>shoppingCartService</c> (correctly mocked); the second call has no CartId and
    /// falls through to <c>GetCart(criteria)</c> → <c>searchService.SearchAllAsync</c> → null →
    /// NullReferenceException on <c>.FirstOrDefault()</c>. Fix: wire the search service to return an
    /// empty result so the second call takes the create-new-cart path (just like CreateAddCartItemsHandler).
    /// </para>
    /// The handler also needs <see cref="IFileUploadService"/> (for file section copy) and
    /// <see cref="ICartProductService"/> (for configured variation re-pricing).
    /// </summary>
    public static ChangeCartCurrencyCommandHandler CreateChangeCartCurrencyHandler(int lineItemCount, CartShape shape)
    {
        var mapper = CreateMapper();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        // PRIMARY cart loaded by CartId via shoppingCartService.GetAsync([id], ...) — fresh per call.
        // DynamicProperties must be initialized (empty list, not null) on each item so that
        // CopyItems → x.DynamicProperties.SelectMany(...) does not throw ArgumentNullException.
        // CartBenchmarkFixtures.CreateCart leaves DynamicProperties null by default; we fix that here.
        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [CreateCartWithDynamicProperties(lineItemCount, shape)]);

        // SECOND load (new-currency cart): SearchAllAsync → empty result → CreateNewCartAggregateAsync.
        // SearchAllAsync calls SearchBatchesAsync → SearchAsync(criteria, clone) → results.Any().
        // ShoppingCartSearchResult must have Results initialized or Any() throws ArgumentNullException.
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult { Results = [] });

        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var repository = new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper, cartProductService.Object),
            shoppingCartSearchService: searchService.Object,
            shoppingCartService: shoppingCartService.Object,
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());

        return new ChangeCartCurrencyCommandHandler(
            repository,
            cartProductService.Object,
            Mock.Of<IFileUploadService>()); // file-section copy — not exercised by fixture carts (no file items)
    }

    /// <summary>
    /// A <c>changeCartCurrency</c> command targeting the benchmark cart, switching to USD (same
    /// currency as the loaded cart). Same-currency re-price still exercises the full CopyItems code
    /// path — both the flat and configured branches — without requiring a second currency in the mock.
    /// </summary>
    public static ChangeCartCurrencyCommand CreateChangeCartCurrencyCommand()
    {
        var command = AbstractTypeFactory<ChangeCartCurrencyCommand>.TryCreateInstance();
        command.NewCurrencyCode = CartBenchmarkFixtures.Currency.Code; // "USD" — same as loaded cart (see gotcha above)

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── mergeCart ───────────────────────────────────────────────────────────────────────────────
    // MERGE GOTCHA: The handler calls GetOrCreateCartFromCommandAsync (loads primary cart via
    // CartId → shoppingCartService.GetByIdAsync) and then GetCartById(request.SecondCartId, ...)
    // (also → shoppingCartService.GetByIdAsync). Both IDs must resolve from the same mock.
    // The shared CreateMutationHarness wires GetAsync([...]) → fresh cart for ANY id list, so both
    // calls return a populated cart. We use SecondCartId = "second-cart" (distinct from "benchmark-cart"
    // so the handler's `secondCart.Id != cartAggr.Id` guard passes and the merge actually runs).
    // DeleteAfterMerge = false avoids a RemoveCartAsync call to the mock (which returns Task via
    // loose mock) — keeping the benchmark focused on the merge compute, not the delete I/O path.
    //
    // FLAG: CreateMutationHarness returns a fresh cart with Id = "benchmark-cart" for ANY GetAsync
    // call because CreateCart hardcodes Id. The second cart loaded via SecondCartId will also have
    // Id = "benchmark-cart", making secondCart.Id == cartAggr.Id → the merge guard fires → skips
    // the merge body, measuring only two loads. To exercise the actual merge, we need a local harness
    // whose GetAsync mock returns a SECOND cart with a distinct Id for the second lookup.

    public static MergeCartCommandHandler CreateMergeCartHandler(int lineItemCount, CartShape shape)
    {
        // LOCAL HARNESS: wire GetAsync to return different carts by cartId so the merge guard passes.
        var mapper = CreateMapper();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        // Return the PRIMARY cart for "benchmark-cart", the SECOND cart for "second-cart".
        // Both are fresh per call so the mutation stays idempotent.
        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string rg, bool clone) =>
            {
                // ids is a singleton list containing the cartId being looked up.
                var id = ids[0];
                var cart = id == SecondCartId
                    ? CreateSecondCart(lineItemCount, shape)
                    : CartBenchmarkFixtures.CreateCart(lineItemCount, shape);

                return [cart];
            });

        var repository = new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper, cartProductService.Object),
            shoppingCartSearchService: Mock.Of<IShoppingCartSearchService>(),
            shoppingCartService: shoppingCartService.Object,
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());

        return new MergeCartCommandHandler(repository);
    }

    /// <summary>The second cart for the merge benchmark — same item count/shape as the primary
    /// but with Id = "second-cart" so the merge guard passes.</summary>
    private static ShoppingCart CreateSecondCart(int lineItemCount, CartShape shape)
    {
        var cart = CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
        cart.Id = SecondCartId;
        // Remap item IDs to avoid key collisions with the primary cart's items in the aggregate.
        // Items is ICollection<LineItem> — enumerate via index on the backing List.
        var i = 0;
        foreach (var item in cart.Items)
        {
            item.Id = $"li2-{i++}";
        }

        return cart;
    }

    private const string SecondCartId = "second-cart";

    /// <summary>
    /// A <c>mergeCart</c> command. DeleteAfterMerge=false avoids a RemoveCartAsync call so the
    /// benchmark measures merge compute only (no delete I/O path).
    /// </summary>
    public static MergeCartCommand CreateMergeCartCommand()
    {
        var command = AbstractTypeFactory<MergeCartCommand>.TryCreateInstance();
        command.SecondCartId = SecondCartId;
        command.DeleteAfterMerge = false;

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── clearCart ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Real <see cref="ClearCartCommandHandler"/> over the shared mutation harness.</summary>
    public static ClearCartCommandHandler CreateClearCartHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>clearCart</c> command targeting the benchmark cart.</summary>
    public static ClearCartCommand CreateClearCartCommand()
    {
        var command = AbstractTypeFactory<ClearCartCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── refreshCart ──────────────────────────────────────────────────────────────────────────────
    // Refresh = load (GetOrCreateCartFromCommandAsync) + SaveCartAsync (recalculates, then saves).
    // The "refresh" is literally just a forced reload+recalc+save — no aggregate-level mutation.
    // Both shapes apply: configured-shape load is heavier (variation product re-price on every load).

    /// <summary>Real <see cref="RefreshCartCommandHandler"/> over the shared mutation harness.</summary>
    public static RefreshCartCommandHandler CreateRefreshCartHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>refreshCart</c> command targeting the benchmark cart.</summary>
    public static RefreshCartCommand CreateRefreshCartCommand()
    {
        var command = AbstractTypeFactory<RefreshCartCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── changePurchaseOrderNumber ────────────────────────────────────────────────────────────────

    /// <summary>Real <see cref="ChangePurchaseOrderNumberCommandHandler"/> over the shared mutation harness.</summary>
    public static ChangePurchaseOrderNumberCommandHandler CreateChangePurchaseOrderNumberHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>changePurchaseOrderNumber</c> command setting a PO number on the benchmark cart.</summary>
    public static ChangePurchaseOrderNumberCommand CreateChangePurchaseOrderNumberCommand()
    {
        var command = AbstractTypeFactory<ChangePurchaseOrderNumberCommand>.TryCreateInstance();
        command.PurchaseOrderNumber = "PO-BENCHMARK-001";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── changeComment (cart-level) ───────────────────────────────────────────────────────────────
    // DISTINCT FROM ChangeCartItemCommentCommandHandler (per-item comment) — this is the CART-LEVEL
    // comment mutation (UpdateCartComment on the aggregate). Both benchmarks coexist; no conflict.

    /// <summary>Real <see cref="ChangeCommentCommandHandler"/> over the shared mutation harness.</summary>
    public static ChangeCommentCommandHandler CreateChangeCommentHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>changeComment</c> command setting a cart-level comment on the benchmark cart.</summary>
    public static ChangeCommentCommand CreateChangeCommentCommand()
    {
        var command = AbstractTypeFactory<ChangeCommentCommand>.TryCreateInstance();
        command.Comment = "Benchmark cart-level comment";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── createCart ───────────────────────────────────────────────────────────────────────────────
    // CreateCartCommandHandler calls CreateNewCartAggregateAsync (no CartId) → repository
    // GetCartAsync(criteria) → search returns empty → builds a new cart aggregate → SaveAsync.
    // Pattern mirrors CreateAddCartItemsHandler (search returns empty → create-new path).
    // Each Handle creates a fresh cart → idempotent without [IterationSetup].
    // LineItemCount has no direct effect (the new cart starts empty) but is kept as a param axis
    // for consistency with other handlers — it parameterises GlobalSetup harness cost.
    // Shape = Flat only (configured items are added by AddCartItems, not CreateCart).

    /// <summary>
    /// Builds the real <see cref="CreateCartCommandHandler"/> over a repository where search
    /// returns empty results (→ create-new path on every Handle call). Each invocation creates a
    /// fresh empty cart, so the benchmark stays idempotent without [IterationSetup].
    /// </summary>
    public static CreateCartCommandHandler CreateCreateCartHandler()
    {
        var mapper = CreateMapper();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        // Empty search result → GetCartAsync(criteria) returns null → CreateNewCartAggregateAsync.
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult());

        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var repository = new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper),
            shoppingCartSearchService: searchService.Object,
            shoppingCartService: Mock.Of<IShoppingCartService>(), // SaveChangesAsync → loose Task (DB write dropped)
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: Mock.Of<IPlatformMemoryCache>(), // never hit — no CartId → skips cache
            fileUploadService: Mock.Of<IFileUploadService>());

        return new CreateCartCommandHandler(repository);
    }

    /// <summary>
    /// A <c>createCart</c> command with NO CartId (so the handler takes the create-new path).
    /// CartId intentionally left empty — WithCartContext is NOT used here (it sets CartId).
    /// </summary>
    public static CreateCartCommand CreateCreateCartCommand()
    {
        var command = AbstractTypeFactory<CreateCartCommand>.TryCreateInstance();
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.CartName = "default";
        // CartId intentionally omitted → create-new path

        return command;
    }

    // ── shared ───────────────────────────────────────────────────────────────────────────────────

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<CartMappingProfile>()).CreateMapper();

    /// <summary>
    /// Returns a cart where every <see cref="LineItem.DynamicProperties"/> is initialised to an empty
    /// array (rather than null). Required for <c>ChangeCartCurrencyCommandHandler.CopyItems</c> which
    /// calls <c>x.DynamicProperties.SelectMany(...)</c> without a null-guard — a null source causes
    /// <see cref="System.ArgumentNullException"/> at runtime.
    /// <para>
    /// <see cref="CartBenchmarkFixtures.CreateCart"/> is the shared fixture and must not be modified;
    /// this wrapper patches the returned cart locally.
    /// </para>
    /// </summary>
    private static ShoppingCart CreateCartWithDynamicProperties(int lineItemCount, CartShape shape)
    {
        var cart = CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
        foreach (var item in cart.Items)
        {
            item.DynamicProperties ??= [];
        }

        return cart;
    }
}
