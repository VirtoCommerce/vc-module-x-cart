using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Mapping;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared fixture builders for the XCart benchmarks. The single design rule here mirrors the
/// L1 mock boundary: everything that does I/O is a mock, everything that is pure compute runs for
/// real. In particular the totals calculator is the real <see cref="DefaultShoppingCartTotalsCalculator"/>
/// — mocking it (as some legacy benchmarks did) measures an almost-empty <c>RecalculateAsync</c>.
/// </summary>
internal static class CartBenchmarkFixtures
{
    public const string StoreId = "benchmark-store";

    public static readonly Currency Currency = new(new Language("en-US"), "USD")
    {
        ExchangeRate = 1m,
        RoundingPolicy = new DefaultMoneyRoundingPolicy(),
    };

    public static Store CreateStore() => new() { Id = StoreId, Settings = [] };

    /// <summary>
    /// Builds a <see cref="CartAggregate"/> with the real totals calculator and all I/O leaves
    /// mocked. <paramref name="mapper"/> is required only by the add path (<c>AddItemAsync</c> maps
    /// <c>CartProduct</c> → <c>LineItem</c>); the recalculate path never maps, so callers that only
    /// recalculate may pass a mock mapper.
    /// </summary>
    public static CartAggregate CreateAggregate(IMapper mapper)
    {
        // The real totals calculator resolves each line item's currency via
        // GetAllCurrenciesAsync().First(c => c.Code == lineItem.Currency) and uses its
        // RoundingPolicy — so the mock must return the fixture currency, not an empty list.
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([Currency]);
        var totalsCalculator = new DefaultShoppingCartTotalsCalculator(currencyService.Object);

        // EvaluatePromotionAsync returns Task<PromotionResult>; a loose mock would yield a null
        // result and NRE on .Rewards in RecalculateAsync. Return an empty result (no rewards).
        var marketingEvaluator = new Mock<IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
            .ReturnsAsync(new PromotionResult());

        return new CartAggregate(
            marketingEvaluator.Object,
            totalsCalculator,
            Mock.Of<IOptionalDependency<ITaxProviderSearchService>>(), // HasValue defaults to false → tax branch skipped
            Mock.Of<ICartProductService>(),
            Mock.Of<IDynamicPropertyUpdaterService>(),
            mapper,
            Mock.Of<IMemberService>(),
            Mock.Of<IGenericPipelineLauncher>(), // Execute returns Task → loose mock yields Task.CompletedTask
            Mock.Of<IConfigurationItemValidator>(),
            Mock.Of<IFileUploadService>(),
            Mock.Of<ICartSharingService>(),
            Mock.Of<ICartValidationContextFactory>());
    }

    /// <summary>
    /// Builds a shopping cart with <paramref name="lineItemCount"/> selected line items of the
    /// given <paramref name="shape"/>. OrganizationId is left empty so RecalculateAsync's
    /// UpdateOrganizationName short-circuits without an I/O call.
    /// </summary>
    public static ShoppingCart CreateCart(int lineItemCount, CartShape shape)
    {
        var items = new List<LineItem>(lineItemCount);

        for (var i = 0; i < lineItemCount; i++)
        {
            var item = new LineItem
            {
                Id = $"li-{i}",
                ProductId = $"product-{i}",
                CatalogId = "catalog",
                Sku = $"SKU-{i}",
                Name = $"Product {i}",
                Currency = Currency.Code,
                Quantity = 2,
                ListPrice = 10m,
                SalePrice = 9m,
                SelectedForCheckout = true,
            };

            if (shape == CartShape.Configured)
            {
                item.IsConfigured = true;
                item.ConfigurationItems = CreateConfigurationItems(i);
            }

            items.Add(item);
        }

        return new ShoppingCart
        {
            Id = "benchmark-cart",
            StoreId = StoreId,
            Currency = Currency.Code,
            Items = items,
            Shipments = [],
            Payments = [],
        };
    }

    private static List<ConfigurationItem> CreateConfigurationItems(int lineItemIndex)
    {
        // Three priced variation items per configured line item — enough object graph to make the
        // configured shape diverge from flat without modelling a full LEO design→garment tree.
        return Enumerable.Range(0, 3).Select(v => new ConfigurationItem
        {
            Id = $"ci-{lineItemIndex}-{v}",
            Type = "Variation",
            ProductId = $"variation-{lineItemIndex}-{v}",
            Quantity = 1,
        }).ToList();
    }

    // ── addCartItems command-level harness ──────────────────────────────────────────────────────

    /// <summary>Real AutoMapper from the production cart profile — the add path maps CartProduct → LineItem.</summary>
    public static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<CartMappingProfile>()).CreateMapper();

    private static CartProduct CreateCartProduct(string productId) =>
        // Active + buyable + no inventory tracking so the Strict add-validation rules pass and the
        // item is actually added (an invalid product makes AddItemAsync return early — measuring
        // nothing). A real ProductPrice drives SetLineItemTierPrice on add.
        new(new CatalogProduct
        {
            Id = productId,
            CatalogId = "catalog",
            Code = $"SKU-{productId}",
            Name = $"Product {productId}",
            IsActive = true,
            IsBuyable = true,
            TrackInventory = false,
        })
        {
            Price = new ProductPrice(Currency)
            {
                ListPrice = new Money(10m, Currency),
                SalePrice = new Money(9m, Currency),
            },
        };

    /// <summary>
    /// Builds the real <see cref="AddCartItemsCommandHandler"/> over a real
    /// <see cref="CartAggregateRepository"/> with every I/O leaf mocked. The search service returns
    /// no carts, so each <c>Handle</c> takes the create-new-cart path (empty CartId) and builds a
    /// fresh aggregate — the benchmark stays idempotent across invocations (no item accumulation),
    /// and the empty-Id cart skips the aggregate cache entirely.
    /// </summary>
    public static AddCartItemsCommandHandler CreateAddCartItemsHandler()
    {
        var mapper = CreateMapper();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CreateStore()]); // GetByIdAsync extension delegates to GetAsync

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([Currency]);

        // Empty result → GetCartAsync returns null → handler creates a new cart each invocation.
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult());

        var cartProductService = new Mock<ICartProductService>();
        cartProductService
            .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string, string)>>()))
            .ReturnsAsync((CartAggregate aggregate, IList<(string CurrencyCode, string ProductId)> pairs) =>
                pairs.ToDictionary(
                    p => aggregate.GetCartProductKey(p.ProductId, p.CurrencyCode),
                    p => CreateCartProduct(p.ProductId)));

        var repository = new CartAggregateRepository(
            cartAggregateFactory: () => CreateAggregate(mapper),
            shoppingCartSearchService: searchService.Object,
            shoppingCartService: Mock.Of<IShoppingCartService>(), // SaveChangesAsync → loose Task (DB write dropped)
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),            // ResolveMemberByIdAsync → null (anonymous)
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: Mock.Of<IPlatformMemoryCache>(),  // never hit — empty-Id carts skip the cache
            fileUploadService: Mock.Of<IFileUploadService>());

        // Empty config search → no active product-configuration → flat-SKU branch (AddItemAsync).
        var configurationSearchService = new Mock<IProductConfigurationSearchService>();
        configurationSearchService
            .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ProductConfigurationSearchResult());

        return new AddCartItemsCommandHandler(
            repository,
            cartProductService.Object,
            Mock.Of<IMediator>(), // only reached on the configured branch, which flat-SKU never takes
            configurationSearchService.Object);
    }

    /// <summary>
    /// A flat-SKU <c>addCartItems</c> command of <paramref name="itemCount"/> items with no CartId
    /// (create-new path). The count is the bulk dimension: 1 = single add, &gt;1 = bulk.
    /// </summary>
    public static AddCartItemsCommand CreateAddCartItemsCommand(int itemCount) =>
        new()
        {
            StoreId = StoreId,
            CurrencyCode = Currency.Code,
            CultureName = "en-US",
            UserId = "benchmark-user",
            CartItems = Enumerable.Range(0, itemCount)
                .Select(i => new NewCartItem($"product-{i}", quantity: 2))
                .ToArray(),
        };
}
