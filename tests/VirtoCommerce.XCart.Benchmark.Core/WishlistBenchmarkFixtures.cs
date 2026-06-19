using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Queries;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Fixture builders for the wishlist command and query benchmarks.
///
/// Wishlist discrimination: the platform's <see cref="ShoppingCart.Type"/> field is set to
/// <c>"Wishlist"</c> on create paths (the handler does <c>request.CartType = CartType.Wishlist</c>
/// before calling <c>CreateNewCartAggregateAsync</c>) and is irrelevant on load-by-id paths
/// (handlers call <c>CartRepository.GetCartByIdAsync(request.ListId)</c> — no Type filter).
/// The shared <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> cart has <c>Type = null</c>;
/// this is fine for load-by-id handlers.
///
/// Two path categories:
/// <list type="bullet">
/// <item><b>Create paths</b> (CreateWishlist, CloneWishlist, CreateCartFromWishlist): no ListId on
/// the target; the handler calls <c>CreateNewCartAggregateAsync</c> → a new in-memory
/// <see cref="ShoppingCart"/> built and passed to
/// <c>CartAggregateRepository.GetCartForShoppingCartAsync</c>. These handlers are wired with a
/// search mock (for CloneWishlist's source load, for CreateCartFromWishlist's
/// <c>GetOrCreateCartFromCommandAsync</c>) and a <c>GetAsync</c> mock that serves the source wishlist.
/// </item>
/// <item><b>Mutate-existing paths</b> (Add/Remove/Rename/Move/Update item, RemoveWishlist): the
/// handler calls <c>CartRepository.GetCartByIdAsync(request.ListId)</c> — the shared
/// <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> never-cache + fresh-cart-per-call
/// design makes every Handle idempotent without [IterationSetup].</item>
/// </list>
///
/// Shape: wishlists don't have a product-configuration semantics distinct from the flat cart;
/// all wishlist benchmarks run over <see cref="CartShape.Flat"/> only. The configured shape
/// is exercised by the cart-item cluster; benchmarking it here would duplicate that cost without
/// illuminating wishlist-specific logic.
///
/// FLAG (centralise): <see cref="CreateWishlistCreateHarness"/> and
/// <see cref="CreateCartFromWishlistHarness"/> mirror the <c>CreateAddCartItemsHandler</c> wiring
/// pattern from <see cref="CartBenchmarkFixtures"/> but are local because the shared class does not
/// expose a generic create-path harness. If a third cluster needs a create harness, extract a
/// <c>CreatePathHarness</c> helper into <c>CartBenchmarkFixtures</c>.
/// </summary>
internal static class WishlistBenchmarkFixtures
{
    // ── Constants ────────────────────────────────────────────────────────────────────────────────

    public const string WishlistId = "benchmark-wishlist";
    public const string DestinationWishlistId = "benchmark-wishlist-dest";
    public const string WishlistName = "My Wishlist";
    public const string WishlistProductId = "wishlist-product-0";

    // ── Wishlist cart factory (local) ────────────────────────────────────────────────────────────

    /// <summary>
    /// A wishlist-typed cart with <paramref name="lineItemCount"/> flat items. Distinct from
    /// <see cref="CartBenchmarkFixtures.CreateCart"/> so the shared fixture stays unmodified.
    /// Id, Name, CustomerId, LanguageCode, Coupons, Shipments, Payments are all populated so the
    /// cart is valid for the load path and passes wishlist validators.
    /// </summary>
    public static ShoppingCart CreateWishlistCart(int lineItemCount)
    {
        var items = new List<LineItem>(lineItemCount);

        for (var i = 0; i < lineItemCount; i++)
        {
            var item = AbstractTypeFactory<LineItem>.TryCreateInstance();
            item.Id = $"li-{i}";
            item.ProductId = $"product-{i}";
            item.CatalogId = "catalog";
            item.Sku = $"SKU-{i}";
            item.Name = $"Product {i}";
            item.Currency = CartBenchmarkFixtures.Currency.Code;
            item.Quantity = 1;
            item.ListPrice = 10m;
            item.SalePrice = 9m;
            item.SelectedForCheckout = true;
            items.Add(item);
        }

        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();
        cart.Id = WishlistId;
        cart.Name = WishlistName;
        cart.StoreId = CartBenchmarkFixtures.StoreId;
        cart.CustomerId = "benchmark-user";
        cart.Currency = CartBenchmarkFixtures.Currency.Code;
        cart.LanguageCode = "en-US";
        cart.Type = "Wishlist";
        cart.Items = items;
        cart.Shipments = [];
        cart.Payments = [];
        cart.Coupons = [];

        return cart;
    }

    // ── WishlistUserContext helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="WishlistUserContext"/> with no scope, so <c>UpdateScopeAsync</c> in
    /// <see cref="ScopedWishlistCommandHandlerBase{TCommand}"/> skips all branches and neither
    /// <c>EnsureSharingSettings</c> nor <c>SetOwner</c> is called. Safe with a loose-mock
    /// <see cref="ICartSharingService"/>.
    /// </summary>
    public static WishlistUserContext EmptyWishlistUserContext() =>
        new()
        {
            CurrentUserId = "benchmark-user",
            UserId = "benchmark-user",
            Scope = null,
        };

    // ── Stamp WishlistCommand context ────────────────────────────────────────────────────────────

    /// <summary>
    /// Stamps the shared cart context onto a <see cref="WishlistCommand"/>: both the
    /// <see cref="CartCommand.CartId"/> (used by the repository's cache key) and the
    /// wishlist-specific <see cref="WishlistCommand.ListId"/> (used by handlers'
    /// <c>GetCartByIdAsync</c> calls). For create-path commands, <c>ListId</c> is the SOURCE
    /// wishlist id; <c>CartId</c> is left empty so the handler's
    /// <c>GetOrCreateCartFromCommandAsync</c> takes the create-new branch.
    /// </summary>
    public static T WithWishlistContext<T>(T command, bool isCreatePath = false)
        where T : WishlistCommand
    {
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.WishlistUserContext = EmptyWishlistUserContext();

        if (isCreatePath)
        {
            // Create-path: no CartId (handler creates new), source ListId for clone/fromwishlist
            command.ListId = WishlistId;
        }
        else
        {
            // Mutate-existing-path: GetCartByIdAsync uses ListId to load the cart
            command.CartId = WishlistId;
            command.ListId = WishlistId;
        }

        return command;
    }

    // ── Create-path harness ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A create-path <see cref="CartAggregateRepository"/> whose <c>GetAsync</c> returns a fresh
    /// wishlist cart per call (for Clone/CreateCartFromWishlist source reads) and whose
    /// <c>SearchAsync</c> returns empty (so <c>GetOrCreateCartFromCommandAsync</c> always creates).
    ///
    /// FLAG: mirrors <c>CreateAddCartItemsHandler</c> wiring in <see cref="CartBenchmarkFixtures"/>;
    /// candidate for extraction into a shared <c>CreatePathHarness</c> helper.
    /// </summary>
    public static CartAggregateRepository CreateWishlistCreateHarness(int lineItemCount)
    {
        var mapper = CartBenchmarkFixtures.CreateMapper();
        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        // Empty search → GetCartAsync returns null → handler creates new cart (CreateWishlist path).
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult());

        // GetAsync → GetByIdAsync: serves the source wishlist for Clone/CreateCartFromWishlist.
        // Returns a fresh cart per call so the benchmark is idempotent across invocations.
        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [CreateWishlistCart(lineItemCount)]);

        return new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper, cartProductService.Object),
            shoppingCartSearchService: searchService.Object,
            shoppingCartService: shoppingCartService.Object,
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());
    }

    // ── Handler factories: create paths ──────────────────────────────────────────────────────────

    /// <summary>Real <see cref="CreateWishlistCommandHandler"/> — create-new-cart path.</summary>
    public static CreateWishlistCommandHandler CreateWishlistHandler() =>
        new(CreateWishlistCreateHarness(0), Mock.Of<ICartSharingService>());

    /// <summary>A <c>createWishlist</c> command with a valid list name and no scope (private by
    /// default). No CartId so the handler creates a new cart each invocation.</summary>
    public static CreateWishlistCommand CreateWishlistCommand() =>
        WithWishlistContext(new CreateWishlistCommand { ListName = WishlistName }, isCreatePath: true);

    /// <summary>Real <see cref="CloneWishlistCommandHandler"/> — reads source wishlist, creates new.</summary>
    public static CloneWishlistCommandHandler CreateCloneWishlistHandler(int lineItemCount) =>
        new(CreateWishlistCreateHarness(lineItemCount),
            // IShoppingCartService.GetByIdAsync is also called directly in Handle if WishlistUserContext.Cart is null.
            // The create harness's shoppingCartService already mocks GetAsync → GetByIdAsync delegates to it.
            Mock.Of<IShoppingCartService>(),
            Mock.Of<ICartSharingService>());

    /// <summary>A <c>cloneWishlist</c> command. <c>ListId</c> is the source wishlist id; no
    /// <c>CartId</c> so the handler creates the clone as a new cart.</summary>
    public static CloneWishlistCommand CreateCloneWishlistCommand() =>
        WithWishlistContext(new CloneWishlistCommand { ListName = WishlistName + " (clone)" }, isCreatePath: true);

    /// <summary>
    /// Real <see cref="CreateCartFromWishlistCommandHandler"/> — reads source wishlist then creates a
    /// new shopping cart. The command carries a <c>ListId</c> for the source and leaves <c>CartId</c>
    /// empty so <c>GetOrCreateCartFromCommandAsync</c> on the secondary command takes the create branch.
    /// </summary>
    public static CreateCartFromWishlistCommandHandler CreateCartFromWishlistHandler(int lineItemCount) =>
        new(CreateWishlistCreateHarness(lineItemCount));

    /// <summary>A <c>createCartFromWishlist</c> command whose source is the fixture wishlist.</summary>
    public static CreateCartFromWishlistCommand CreateCartFromWishlistCommand() =>
        WithWishlistContext(new CreateCartFromWishlistCommand(), isCreatePath: true);

    // ── Handler factories: mutate-existing paths ──────────────────────────────────────────────────

    /// <summary>Real <see cref="AddWishlistItemCommandHandler"/> — loads the wishlist, adds an item.</summary>
    public static AddWishlistItemCommandHandler CreateAddWishlistItemHandler(int lineItemCount)
    {
        var harness = CreateWishlistMutationHarness(lineItemCount);
        var configurationSearchService = new Mock<IProductConfigurationSearchService>();
        // No active product configuration → plain AddItemsAsync (flat) branch — the benchmarked path.
        configurationSearchService
            .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ProductConfigurationSearchResult());

        return new AddWishlistItemCommandHandler(
            harness.Repository,
            configurationSearchService.Object,
            harness.CartProductService,
            Mock.Of<IMediator>());
    }

    /// <summary>An <c>addWishlistItem</c> command with a valid product ID and default quantity 1.</summary>
    public static AddWishlistItemCommand CreateAddWishlistItemCommand() =>
        WithWishlistContext(new AddWishlistItemCommand { ProductId = WishlistProductId, Quantity = 1 });

    /// <summary>Real <see cref="RenameWishlistCommandHandler"/> — loads the wishlist, renames it.</summary>
    public static RenameWishlistCommandHandler CreateRenameWishlistHandler(int lineItemCount) =>
        new(CreateWishlistMutationHarness(lineItemCount).Repository);

    /// <summary>A <c>renameWishlist</c> command with a new list name. The ctor requires both
    /// <c>listId</c> and <c>listName</c>; <see cref="WithWishlistContext{T}"/> overwrites
    /// <c>ListId</c> to <c>WishlistId</c> after construction.</summary>
    public static RenameWishlistCommand CreateRenameWishlistCommand() =>
        WithWishlistContext(new RenameWishlistCommand(WishlistId, WishlistName + " (renamed)"));

    /// <summary>
    /// Real <see cref="ChangeWishlistCommandHandler"/> — loads the wishlist, updates name/description
    /// and scope. We benchmark the no-scope branch (Scope = null) so <c>UpdateScopeAsync</c> is a
    /// no-op and the measured cost is the load + field mutation + save.
    /// </summary>
    public static ChangeWishlistCommandHandler CreateChangeWishlistHandler(int lineItemCount) =>
        new(CreateWishlistMutationHarness(lineItemCount).Repository, Mock.Of<ICartSharingService>());

    /// <summary>A <c>changeWishlist</c> command that renames the list. No scope → private path (no-op
    /// in UpdateScopeAsync — Scope = null skips all branches).</summary>
    public static ChangeWishlistCommand CreateChangeWishlistCommand() =>
        WithWishlistContext(new ChangeWishlistCommand { ListName = WishlistName + " (changed)", Scope = null });

    /// <summary>Real <see cref="RemoveWishlistCommandHandler"/> — deletes the wishlist by id.</summary>
    public static RemoveWishlistCommandHandler CreateRemoveWishlistHandler() =>
        // RemoveCartAsync only calls IShoppingCartService.DeleteAsync → the repository's shoppingCartService
        // loose mock returns Task.CompletedTask for any DeleteAsync call.
        new(CreateWishlistMutationHarness(0).Repository);

    /// <summary>A <c>removeWishlist</c> command for the fixture wishlist. The ctor requires
    /// <c>listId</c>; <see cref="WithWishlistContext{T}"/> overwrites it to <c>WishlistId</c>.</summary>
    public static RemoveWishlistCommand CreateRemoveWishlistCommand() =>
        WithWishlistContext(new RemoveWishlistCommand(WishlistId));

    /// <summary>Real <see cref="RemoveWishlistItemCommandHandler"/> — loads the wishlist, removes by
    /// line item id (the first item, <c>li-0</c>).</summary>
    public static RemoveWishlistItemCommandHandler CreateRemoveWishlistItemHandler(int lineItemCount) =>
        new(CreateWishlistMutationHarness(lineItemCount).Repository);

    /// <summary>A <c>removeWishlistItem</c> command targeting the first line item. The benchmark
    /// measures the load + remove + recalc path; the harness returns a fresh cart each call so
    /// the item is always present.</summary>
    public static RemoveWishlistItemCommand CreateRemoveWishlistItemCommand() =>
        WithWishlistContext(new RemoveWishlistItemCommand { LineItemId = "li-0" });

    /// <summary>Real <see cref="MoveWishListItemCommandHandler"/> — loads source + destination wishlists,
    /// moves one item. Both carts are served by the shared harness (a single mock that returns fresh carts
    /// for any GetAsync call).</summary>
    public static MoveWishListItemCommandHandler CreateMoveWishlistItemHandler(int lineItemCount) =>
        new(CreateMoveWishlistHarness(lineItemCount));

    /// <summary>A <c>moveWishlistItem</c> command moving <c>li-0</c> from the source to the destination
    /// wishlist. The ctor requires all three ids; <see cref="WithWishlistContext{T}"/> overwrites
    /// <c>ListId</c> to <c>WishlistId</c> after construction.</summary>
    public static MoveWishlistItemCommand CreateMoveWishlistItemCommand() =>
        WithWishlistContext(new MoveWishlistItemCommand(WishlistId, DestinationWishlistId, "li-0"));

    /// <summary>Real <see cref="UpdateWishlistItemsCommandHandler"/> — loads the wishlist, updates
    /// quantities for all items.</summary>
    public static UpdateWishlistItemsCommandHandler CreateUpdateWishlistItemsHandler(int lineItemCount)
    {
        var harness = CreateWishlistMutationHarness(lineItemCount);
        return new UpdateWishlistItemsCommandHandler(harness.Repository, harness.CartProductService);
    }

    /// <summary>An <c>updateWishlistItems</c> command updating the quantity of the first item to 3.
    /// The handler loops over the command's Items and calls <c>ChangeItemQuantityAsync</c> per item;
    /// the benchmark covers the single-item update so the item-count axis drives cart size, not
    /// the number of items updated.</summary>
    public static UpdateWishlistItemsCommand CreateUpdateWishlistItemsCommand() =>
        WithWishlistContext(new UpdateWishlistItemsCommand
        {
            Items = [new WishListItem { LineItemId = "li-0", Quantity = 3 }],
        });

    // ── Query handler factories ───────────────────────────────────────────────────────────────────

    /// <summary>Real <see cref="GetWishlistQueryHandler"/> — loads the wishlist by id via
    /// <c>GetCartByIdAsync</c>. The mutation harness serves a fresh cart per call.</summary>
    public static GetWishlistQueryHandler CreateGetWishlistHandler(int lineItemCount) =>
        new(CreateWishlistMutationHarness(lineItemCount).Repository);

    /// <summary>A <c>getWishlist</c> query for the fixture wishlist.</summary>
    public static GetWishlistQuery CreateGetWishlistQuery() =>
        new()
        {
            ListId = WishlistId,
            CultureName = "en-US",
            IncludeFields = [],
        };

    /// <summary>Real <see cref="SearchWishlistQueryHandler"/> — searches carts via
    /// <c>SearchCartAsync</c>. The search service returns an empty result set so the handler
    /// performs the build step with zero aggregates — measuring the search-criteria build +
    /// dispatch overhead without the per-cart recalc cost.
    ///
    /// <c>ISavedForLaterListService</c> is accepted by the ctor but not called during
    /// <c>Handle</c>; a loose mock is sufficient.
    /// </summary>
    public static SearchWishlistQueryHandler CreateSearchWishlistHandler()
    {
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult { Results = [], TotalCount = 0 });

        var mapper = CartBenchmarkFixtures.CreateMapper();
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var repository = new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(CartBenchmarkFixtures.CreateMapper()),
            shoppingCartSearchService: searchService.Object,
            shoppingCartService: Mock.Of<IShoppingCartService>(),
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: CartBenchmarkFixtures.CartProductServiceMock().Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());

        return new SearchWishlistQueryHandler(
            repository,
            mapper,
            Mock.Of<ISearchPhraseParser>(),
            Mock.Of<ISavedForLaterListService>());
    }

    /// <summary>A <c>searchWishlists</c> query with minimal required fields.</summary>
    public static SearchWishlistQuery CreateSearchWishlistQuery() =>
        new()
        {
            StoreId = CartBenchmarkFixtures.StoreId,
            UserId = "benchmark-user",
            CurrencyCode = CartBenchmarkFixtures.Currency.Code,
            CultureName = "en-US",
            IncludeFields = [],
            Take = 20,
        };

    // ── Private harness builders ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shared mutate-existing-cart harness for wishlist handlers. Wraps
    /// <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> but overrides the
    /// <c>GetAsync</c> mock to return a wishlist-typed cart (id = <c>benchmark-wishlist</c>)
    /// so handlers' <c>GetCartByIdAsync(request.ListId)</c> resolve correctly.
    ///
    /// FLAG: the shared <c>CreateMutationHarness</c> hard-codes <c>Id = "benchmark-cart"</c>.
    /// Wishlist handlers address the cart by <c>ListId = "benchmark-wishlist"</c>, so a local
    /// harness with the matching id is required. If the shared fixture is extended to accept a
    /// custom cart factory, remove <c>CreateWishlistMutationHarness</c>.
    /// </summary>
    private static CartBenchmarkFixtures.MutationHarness CreateWishlistMutationHarness(int lineItemCount)
    {
        var mapper = CartBenchmarkFixtures.CreateMapper();
        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        // Return a fresh wishlist cart per call so each Handle loads its own instance.
        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [CreateWishlistCart(lineItemCount)]);

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

        return new CartBenchmarkFixtures.MutationHarness
        {
            Repository = repository,
            CartProductService = cartProductService.Object,
        };
    }

    /// <summary>
    /// Harness for <see cref="MoveWishListItemCommandHandler"/>: loads TWO carts (source ListId +
    /// destination DestinationListId). A single <c>GetAsync</c> mock returns fresh wishlist carts
    /// regardless of which id is requested; the destination cart also gets items so
    /// <c>AddItemsAsync</c> has a non-empty context on save.
    /// </summary>
    private static CartAggregateRepository CreateMoveWishlistHarness(int lineItemCount)
    {
        var mapper = CartBenchmarkFixtures.CreateMapper();
        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        // Both source and destination loads come through GetAsync; return a fresh cart per call.
        // The destination is a fresh wishlist with some items (so move destination is valid).
        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() =>
            {
                // Return a fresh wishlist cart regardless of which id was requested.
                return [CreateWishlistCart(lineItemCount)];
            });

        return new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper, cartProductService.Object),
            shoppingCartSearchService: Mock.Of<IShoppingCartSearchService>(),
            shoppingCartService: shoppingCartService.Object,
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());
    }
}
