using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Builds the benchmark's <see cref="IServiceProvider"/> the same way the cart module is wired in
/// production — so a command/query <b>handler</b> (and any module override of it) is resolved through
/// MediatR exactly as it ships, not hand-constructed. The single design rule is unchanged from the
/// hand-built fixtures: everything that does I/O is a mock, everything that is pure compute runs for
/// real (the totals calculator above all).
///
/// <para>Composition order matters for overrides: the base XCart handlers are registered first (MediatR
/// assembly scan), the shared mocked I/O leaves next, and the active module setup's
/// <see cref="ICartBenchmarkSetup.ConfigureServices"/> <b>last</b> — so a consumer's
/// <c>OverrideCommandType</c>/<c>UseCommandType().WithCommandHandler()</c> registration (a later
/// <c>AddTransient(IRequestHandler&lt;,&gt;)</c>) wins by DI last-registration semantics, and its
/// <c>AbstractTypeFactory</c> + aggregate + recalculate-pipeline overrides take effect. The benchmark
/// then resolves <see cref="IMediator"/> and sends a factory-built command, so the consumer's overridden
/// command type and handler are what actually run.</para>
/// </summary>
public static class CartBenchmarkHost
{
    /// <summary>
    /// Composes a provider for a cart of <paramref name="lineItemCount"/> items of the given
    /// <paramref name="shape"/>. The mocked <see cref="IShoppingCartService"/> returns a FRESH cart on
    /// every <c>GetAsync</c> so a mutation never accumulates across invocations, and the never-cache
    /// forces a real load+recalc each call — the same idempotency contract the hand-built mutation
    /// harness provided.
    /// </summary>
    public static IServiceProvider BuildProvider(
        ICartBenchmarkSetup setup,
        int lineItemCount,
        CartShape shape,
        Action<ShoppingCart> customizeCart = null,
        Action<IServiceCollection> customizeServices = null)
    {
        var services = new ServiceCollection();

        // Base handlers — register MediatR from the Core AND Data assemblies, mirroring the module's
        // production AddSchema(typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker)). Anchor on the
        // assembly marker types, not specific handlers that could be renamed/removed.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(CoreAssemblyMarker).Assembly,
            typeof(DataAssemblyMarker).Assembly));

        // ── Real compute ────────────────────────────────────────────────────────────────────────
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);
        services.AddSingleton(currencyService.Object);
        services.AddSingleton<IShoppingCartTotalsCalculator>(sp =>
            new DefaultShoppingCartTotalsCalculator(sp.GetRequiredService<ICurrencyService>()));

        // The add path maps CartProduct → LineItem with the real production profile.
        services.AddSingleton(CartBenchmarkFixtures.CreateMapper());

        // ── Mocked I/O leaves ───────────────────────────────────────────────────────────────────
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);
        services.AddSingleton(storeService.Object);

        // GetByIdAsync(cartId, rg) → GetAsync([cartId], rg, clone): a fresh populated cart per call so
        // each Handle loads its own instance (mutation stays idempotent, no [IterationSetup] needed).
        // The returned cart's Id mirrors the REQUESTED id (as a real service does), so a handler that
        // loads two carts and branches on their ids — mergeCart's secondCart.Id != cartAggr.Id guard —
        // sees distinct carts and actually runs the merge body rather than short-circuiting.
        var cartService = new Mock<IShoppingCartService>();
        cartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) =>
            {
                // The consumer's graph if it supplies one (real recalc/validate work), else Core's generic shape.
                var cart = setup.CreateCart(lineItemCount, shape) ?? CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
                if (ids is { Count: > 0 })
                {
                    cart.Id = ids[0];
                }
                customizeCart?.Invoke(cart);

                return [cart];
            });
        services.AddSingleton(cartService.Object);

        // Empty (non-null) search result: the CartId load path never searches, but SearchAllAsync reads
        // .Results, and the create-new add path (empty CartId) takes the new-cart branch on an empty result.
        var searchService = new Mock<IShoppingCartSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartSearchResult());
        services.AddSingleton(searchService.Object);

        services.AddSingleton(CartBenchmarkFixtures.CartProductServiceMock().Object);
        services.AddSingleton(CartBenchmarkFixtures.NeverCacheMock().Object);
        services.AddSingleton(Mock.Of<IMemberResolver>());        // anonymous
        services.AddSingleton(Mock.Of<IMemberService>());
        services.AddSingleton(Mock.Of<IFileUploadService>());
        services.AddSingleton(Mock.Of<ICartSharingService>());
        services.AddSingleton(Mock.Of<IDynamicPropertyUpdaterService>());
        services.AddSingleton(Mock.Of<ICartValidationContextFactory>());
        services.AddSingleton(Mock.Of<IOptionalDependency<ITaxProviderSearchService>>()); // HasValue false → tax branch skipped

        // EvaluatePromotionAsync → empty PromotionResult (loose mock would NRE on .Rewards).
        var marketingEvaluator = new Mock<IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
            .ReturnsAsync(new PromotionResult());
        services.AddSingleton(marketingEvaluator.Object);

        // AddConfiguredItemAsync validates the configured line item → return a passing result.
        var configurationItemValidator = new Mock<IConfigurationItemValidator>();
        configurationItemValidator
            .Setup(x => x.ValidateAsync(It.IsAny<LineItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        services.AddSingleton(configurationItemValidator.Object);

        services.AddSingleton(CreateProductConfigurationSearchService(shape));

        // Compute services the handlers need, real where pure-compute over the leaves above, mocked
        // where they front I/O (shipping/payment/gift availability, saved-for-later persistence, phrase
        // parsing) — matching the per-scenario mock choices the hand-built fixtures made.
        services.AddTransient<IConfiguredLineItemContainerService, ConfiguredLineItemContainerService>();
        services.AddSingleton<ICartResponseGroupParser, CartResponseGroupParser>();
        services.AddSingleton(new Mock<ICartAvailMethodsService> { DefaultValue = DefaultValue.Mock }.Object);
        services.AddSingleton(Mock.Of<ISavedForLaterListService>());
        services.AddSingleton(Mock.Of<ISearchPhraseParser>());
        services.AddSingleton(Mock.Of<IPickupLocationService>());
        services.AddSingleton(Mock.Of<ICustomerPreferenceService>());

        // The real CreateConfiguredLineItemHandler loads the configurable product via this service —
        // return one priced product per requested id so the configured add path produces a real item.
        var loader = new Mock<ICartProductsLoaderService>();
        loader
            .Setup(x => x.GetCartProductsAsync(It.IsAny<CartProductsRequest>()))
            .ReturnsAsync((CartProductsRequest request) =>
                (request.ProductIds ?? []).Select(CartBenchmarkFixtures.CreateCartProduct).ToList());
        services.AddSingleton(loader.Object);

        // ── Aggregate + repository (mirror AddXCart) ──────────────────────────────────────────────
        // CartAggregate is registered by the module setup (base vs subclass); Core registers the
        // factory + repository over it. The setup also supplies the pipeline launcher (upstream mocks
        // it, a consumer provides a real one).
        services.AddTransient<Func<CartAggregate>>(provider =>
            () => provider.CreateScope().ServiceProvider.GetRequiredService<CartAggregate>());
        services.AddTransient<ICartAggregateRepository, CartAggregateRepository>();

        // ── Consumer overrides, then per-op scenario overrides — last wins by DI last-registration ──
        setup.ConfigureServices(services);
        customizeServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    // A config per requested product (Configured) so the add path routes through the configured
    // branch; an empty result (Flat) so it takes the plain AddItemAsync branch.
    private static IProductConfigurationSearchService CreateProductConfigurationSearchService(CartShape shape)
    {
        var mock = new Mock<IProductConfigurationSearchService>();
        if (shape == CartShape.Configured)
        {
            mock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync((ProductConfigurationSearchCriteria criteria, bool _) =>
                {
                    var results = (criteria.ProductIds ?? [])
                        .Select(productId => new ProductConfiguration { ProductId = productId })
                        .ToList();
                    return new ProductConfigurationSearchResult { Results = results, TotalCount = results.Count };
                });
        }
        else
        {
            mock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult());
        }

        return mock.Object;
    }
}
