using System.Collections.Generic;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Services;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Fixture builders for the Gifts / SavedForLater / DynamicProperties cluster. Design rule (shared
/// with <see cref="CartBenchmarkFixtures"/>): everything that does I/O is mocked at the leaf, pure
/// compute runs for real.
///
/// <b>Gifts</b>: <see cref="ICartAvailMethodsService.GetAvailableGiftsAsync"/> is the I/O leaf (it
/// evaluates promotions) and is mocked to return ONE available gift whose Id matches the command —
/// so <see cref="CartAggregate.AddGiftItemsAsync"/> takes the real add branch (maps the gift to a
/// <c>GiftLineItem</c> via the real mapper, attaches a promotion discount, adds it to the cart) and
/// the cart recalculates with the gift present. RejectGiftCartItems likewise targets a real gift.
///
/// <b>SavedForLater</b>: the <b>real</b> <see cref="SavedForLaterListService"/> runs — load the cart,
/// find/create the saved-for-later list, copy the items across, remove from the source, save both
/// (two real recalculates). Only the DB read/write leaves are mocked. MoveTo's list lookup returns
/// empty (→ create a fresh list); MoveFrom's lookup returns a seeded saved-for-later list holding the
/// item to move back.
///
/// <b>DynamicProperties</b>: <see cref="IDynamicPropertyUpdaterService"/> (which loads dynamic-property
/// metadata — I/O) is the loose mock leaf; the handler's real work is load + apply + save (recalc).
/// </summary>
internal static class GiftsSavedDynamicBenchmarkFixtures
{
    // ── Gifts ─────────────────────────────────────────────────────────────────────────────────────

    public const string GiftId = "benchmark-gift";

    /// <summary>One available gift the add path will accept: its Id matches the command, it carries a
    /// (mocked) non-null <see cref="Promotion"/> — <c>AddGiftItemsAsync</c> reads <c>Promotion.Id</c> —
    /// and product/catalog/sku so the mapped <see cref="GiftLineItem"/> is a valid cart line item.</summary>
    private static GiftItem CreateAvailableGift() =>
        new()
        {
            Id = GiftId,
            ProductId = "gift-product",
            CatalogId = "catalog",
            Sku = "GIFT-SKU",
            Name = "Benchmark Gift",
            Quantity = 1,
            Coupon = null,
            Promotion = Mock.Of<Promotion>(),
        };

    private static Mock<ICartAvailMethodsService> AvailGiftsMock()
    {
        var mock = new Mock<ICartAvailMethodsService>();
        mock.Setup(x => x.GetAvailableGiftsAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync(() => [CreateAvailableGift()]);
        return mock;
    }

    /// <summary>Real <see cref="AddGiftItemsCommandHandler"/> over the shared mutation harness, with an
    /// avail-gifts mock that offers one matching gift so the real add branch runs.</summary>
    public static AddGiftItemsCommandHandler CreateAddGiftItemsHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository, AvailGiftsMock().Object);

    /// <summary>An <c>addGiftItems</c> command requesting the one available gift by id.</summary>
    public static AddGiftItemsCommand CreateAddGiftItemsCommand()
    {
        var command = AbstractTypeFactory<AddGiftItemsCommand>.TryCreateInstance();
        command.Ids = [GiftId];

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>Real <see cref="RejectGiftCartItemsCommandHandler"/> over the shared mutation harness.</summary>
    public static RejectGiftCartItemsCommandHandler CreateRejectGiftCartItemsHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>rejectGiftCartItems</c> command. The loaded cart carries no gift line items, so the
    /// reject scan finds nothing and returns — the success no-op path over the real load+recalc+save.</summary>
    public static RejectGiftCartItemsCommand CreateRejectGiftCartItemsCommand()
    {
        var command = AbstractTypeFactory<RejectGiftCartItemsCommand>.TryCreateInstance();
        command.Ids = [GiftId];

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── SavedForLater (real service) ────────────────────────────────────────────────────────────────

    private static void StampMoveContext(MoveSavedForLaterItemsCommandBase command)
    {
        command.CartId = "benchmark-cart";
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.LineItemIds = ["li-0"];
    }

    /// <summary>A saved-for-later list cart holding a single flat line item (li-0) to move back into the
    /// cart. Fresh per call so MoveFrom never accumulates.</summary>
    private static ShoppingCart CreateSavedForLaterCart()
    {
        var cart = CartBenchmarkFixtures.CreateCart(1, CartShape.Flat);
        cart.Id = "saved-for-later-list";
        cart.Type = CartType.SavedForLater;

        return cart;
    }

    /// <summary>
    /// Builds the real <see cref="SavedForLaterListService"/> over a real <see cref="CartAggregateRepository"/>.
    /// The by-id load returns the working cart (benchmark-cart); the list search returns
    /// <paramref name="seededSavedForLaterCart"/> (MoveFrom) or empty (MoveTo → the service creates one).
    /// A fresh cart per call keeps each move idempotent.
    /// </summary>
    private static SavedForLaterListService CreateSavedForLaterService(int lineItemCount, CartShape shape, bool seedSavedForLaterList)
    {
        var mapper = CartBenchmarkFixtures.CreateMapper();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [CartBenchmarkFixtures.CreateCart(lineItemCount, shape)]);

        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(() => seedSavedForLaterList
                ? new ShoppingCartSearchResult { Results = [CreateSavedForLaterCart()], TotalCount = 1 }
                : new ShoppingCartSearchResult());

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

        return new SavedForLaterListService(repository, cartProductService.Object, Mock.Of<IFileUploadService>());
    }

    /// <summary>Real <see cref="MoveToSavedForLaterItemsCommandHandler"/> over the real saved-for-later
    /// service: load cart → create a fresh saved-for-later list → move li-0 into it → save both.</summary>
    public static MoveToSavedForLaterItemsCommandHandler CreateMoveToSavedForLaterHandler(int lineItemCount, CartShape shape) =>
        new(CreateSavedForLaterService(lineItemCount, shape, seedSavedForLaterList: false));

    public static MoveToSavedForLaterItemsCommand CreateMoveToSavedForLaterCommand()
    {
        var command = AbstractTypeFactory<MoveToSavedForLaterItemsCommand>.TryCreateInstance();
        StampMoveContext(command);

        return command;
    }

    /// <summary>Real <see cref="MoveFromSavedForLaterItemsCommandHandler"/> over the real saved-for-later
    /// service: load cart → find the seeded saved-for-later list → move li-0 back into the cart → save both.</summary>
    public static MoveFromSavedForLaterItemsCommandHandler CreateMoveFromSavedForLaterHandler(int lineItemCount, CartShape shape) =>
        new(CreateSavedForLaterService(lineItemCount, shape, seedSavedForLaterList: true));

    public static MoveFromSavedForLaterItemsCommand CreateMoveFromSavedForLaterCommand()
    {
        var command = AbstractTypeFactory<MoveFromSavedForLaterItemsCommand>.TryCreateInstance();
        StampMoveContext(command);

        return command;
    }

    // ── DynamicProperties ─────────────────────────────────────────────────────────────────────────

    private static IList<DynamicPropertyValue> CreateDynamicPropertyValues() =>
    [
        new DynamicPropertyValue { Name = "benchmark-prop", Value = "benchmark-value" },
    ];

    /// <summary>Real <see cref="UpdateCartDynamicPropertiesCommandHandler"/> over the shared mutation
    /// harness. The dynamic-property updater (metadata I/O) is the loose-mock leaf; the measured work is
    /// load + apply + save (recalc).</summary>
    public static UpdateCartDynamicPropertiesCommandHandler CreateUpdateCartDynamicPropertiesHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    public static UpdateCartDynamicPropertiesCommand CreateUpdateCartDynamicPropertiesCommand()
    {
        var command = AbstractTypeFactory<UpdateCartDynamicPropertiesCommand>.TryCreateInstance();
        command.DynamicProperties = CreateDynamicPropertyValues();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>Real <see cref="UpdateCartItemDynamicPropertiesCommandHandler"/> over the shared mutation
    /// harness, targeting the first line item (li-0).</summary>
    public static UpdateCartItemDynamicPropertiesCommandHandler CreateUpdateCartItemDynamicPropertiesHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    public static UpdateCartItemDynamicPropertiesCommand CreateUpdateCartItemDynamicPropertiesCommand()
    {
        var command = AbstractTypeFactory<UpdateCartItemDynamicPropertiesCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.DynamicProperties = CreateDynamicPropertyValues();

        return CartBenchmarkFixtures.WithCartContext(command);
    }
}
