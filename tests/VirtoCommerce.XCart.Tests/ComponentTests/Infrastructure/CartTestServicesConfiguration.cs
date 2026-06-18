using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using GraphQL;
using GraphQL.Introspection;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CartModule.Data.Repositories;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.OutlinePart;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Search;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CatalogModule.Data.Validation;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.InventoryModule.Core.Model;
using VirtoCommerce.InventoryModule.Core.Model.Search;
using VirtoCommerce.InventoryModule.Core.Services;
using VirtoCommerce.LuceneSearchModule.Data;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Services;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.StoreModule.Data.Repositories;
using VirtoCommerce.StoreModule.Data.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Data.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Data.Index;
using VirtoCommerce.XCatalog.Data.Middlewares;
using VirtoCommerce.XCart.Tests.ComponentTests;
using Aggregation = VirtoCommerce.CatalogModule.Core.Model.Search.Aggregation;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Configures all services needed for X-Cart GraphQL component tests, backed by real in-memory
    /// SQLite databases (Cart + Catalog + Pricing + Store) and a real in-memory Lucene search index.
    /// Mirrors the LEO component-test harness, translated to base VirtoCommerce types and scoped to the
    /// cart/catalog/pricing/store surface that the cart GraphQL flow exercises.
    /// </summary>
    internal static class CartTestServicesConfiguration
    {
        /// <summary>
        /// Registers DB contexts, repository factories, real catalog/cart/pricing/store/search services,
        /// the Lucene provider, the SearchProductResponse pipeline (prices + inventory) and the mocked
        /// external services. Call once per test ServiceCollection.
        /// </summary>
        public static IServiceCollection AddXCartComponentTestServices(this IServiceCollection services)
        {
            // ---------- Database contexts (resolvable; new instance per scope) ----------
            services.AddScoped<CartDbContext>();
            services.AddScoped<CatalogDbContext>();
            services.AddScoped<PricingDbContext>();
            services.AddScoped<StoreDbContext>();

            // ---------- Raw DB commands (SQLite-compatible) ----------
            services.AddScoped<ICartRawDatabaseCommand, SqliteCartRawDatabaseCommand>();
            services.AddScoped<ICatalogRawDatabaseCommand, SqliteCatalogRawDatabaseCommand>();

            // ---------- Repository factories (a NEW DbContext each call, exactly like the platform) ----------
            services.AddScoped<Func<ICatalogRepository>>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<CatalogDbContext>>();
                var rawCommand = sp.GetRequiredService<ICatalogRawDatabaseCommand>();

                return () => new CatalogRepositoryImpl(new CatalogDbContext(options), rawCommand);
            });
            services.AddScoped<ICatalogRepository>(sp => sp.GetRequiredService<Func<ICatalogRepository>>()());

            services.AddScoped<Func<ICartRepository>>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<CartDbContext>>();
                var rawCommand = sp.GetRequiredService<ICartRawDatabaseCommand>();

                return () => new CartRepository(new CartDbContext(options), rawCommand);
            });
            services.AddScoped<ICartRepository>(sp => sp.GetRequiredService<Func<ICartRepository>>()());

            services.AddScoped<Func<IPricingRepository>>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<PricingDbContext>>();

                return () => new PricingRepositoryImpl(new PricingDbContext(options));
            });

            services.AddScoped<Func<IStoreRepository>>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<StoreDbContext>>();

                return () => new StoreRepository(new StoreDbContext(options));
            });

            // ---------- Platform infrastructure ----------
            services.AddScoped<IPlatformMemoryCache>(_ => MemoryCacheMockUtils.GetPlatformMemoryCache());
            services.AddSingleton(Options.Create(new CrudOptions()));
            services.AddLogging();

            // AutoMapper — real profiles for cart + catalog projection.
            services.AddSingleton<IMapper>(sp =>
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<VirtoCommerce.XCart.Data.Mapping.CartMappingProfile>();
                    cfg.AddProfile<VirtoCommerce.XCatalog.Data.Mapping.ProductMappingProfile>();
                });

                return config.CreateMapper();
            });

            // ---------- Mocks: external / irrelevant to these tests ----------
            services.AddScoped<IEventPublisher>(_ => new Mock<IEventPublisher>().Object);
            services.AddScoped<IBlobUrlResolver>(_ => new Mock<IBlobUrlResolver>().Object);
            services.AddScoped<IBlobStorageProvider>(_ => new Mock<IBlobStorageProvider>().Object);

            // File upload — returns no files (configured items / attachments are not exercised by these tests).
            services.AddScoped<IFileUploadService>(_ =>
            {
                var mock = new Mock<IFileUploadService>();
                mock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync([]);

                return mock.Object;
            });

            services.AddScoped<ICurrencyService>(_ =>
            {
                var mock = new Mock<ICurrencyService>();
                mock.Setup(x => x.GetAllCurrenciesAsync())
                    .ReturnsAsync([new Currency { Code = TestConstants.Currency, RoundingPolicy = new DefaultMoneyRoundingPolicy() }]);

                return mock.Object;
            });

            services.AddScoped<IMemberResolver>(_ =>
            {
                var mock = new Mock<IMemberResolver>();
                mock.Setup(x => x.ResolveMemberByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((string id) => new Contact { Id = id, Name = "Test User", Groups = [], Addresses = [] });

                return mock.Object;
            });

            // Store currency resolver — single USD currency (used by SearchProductQueryHandler).
            services.AddScoped<IStoreCurrencyResolver>(_ =>
            {
                var language = new Language(TestConstants.LanguageCode);
                var currency = new Currency(language, TestConstants.Currency)
                {
                    Symbol = "$",
                    ExchangeRate = 1m,
                    RoundingPolicy = new DefaultMoneyRoundingPolicy(),
                };

                var mock = new Mock<IStoreCurrencyResolver>();
                mock.Setup(x => x.GetStoreCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(currency);
                mock.Setup(x => x.GetAllStoreCurrenciesAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync([currency]);

                return mock.Object;
            });
            services.AddScoped<IMemberService>(_ =>
            {
                var mock = new Mock<IMemberService>();
                mock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((string id, string _, string _) => new Contact { Id = id, Name = "Test User", Groups = [], Addresses = [] });

                return mock.Object;
            });

            services.AddScoped<IMarketingPromoEvaluator>(_ =>
            {
                var mock = new Mock<IMarketingPromoEvaluator>();
                mock.Setup(x => x.EvaluatePromotionAsync(It.IsAny<IEvaluationContext>()))
                    .ReturnsAsync(new PromotionResult());

                return mock.Object;
            });

            services.AddScoped<IOptionalDependency<ITaxProviderSearchService>>(_ =>
            {
                var mock = new Mock<IOptionalDependency<ITaxProviderSearchService>>();
                mock.Setup(x => x.HasValue).Returns(false);

                return mock.Object;
            });

            services.AddScoped<IPaymentMethodsSearchService>(_ =>
            {
                var mock = new Mock<IPaymentMethodsSearchService>();
                mock.Setup(x => x.SearchAsync(It.IsAny<PaymentMethodsSearchCriteria>(), It.IsAny<bool>()))
                    .ReturnsAsync(new PaymentMethodsSearchResult());

                return mock.Object;
            });
            services.AddScoped<IShippingMethodsSearchService>(_ =>
            {
                var mock = new Mock<IShippingMethodsSearchService>();
                mock.Setup(x => x.SearchAsync(It.IsAny<VirtoCommerce.ShippingModule.Core.Model.Search.ShippingMethodsSearchCriteria>(), It.IsAny<bool>()))
                    .ReturnsAsync(new VirtoCommerce.ShippingModule.Core.Model.Search.ShippingMethodsSearchResult());

                return mock.Object;
            });

            // Settings manager — GetObjectSettingAsync returns null (use defaults).
            services.AddScoped<ISettingsManager>(_ =>
            {
                var mock = new Mock<ISettingsManager>();
                mock.Setup(x => x.GetObjectSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((ObjectSettingEntry?)null);
                mock.Setup(x => x.GetSettingsForType(It.IsAny<string>())).Returns([]);
                mock.Setup(x => x.GetObjectSettingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync((IEnumerable<string> names, string objectType, string objectId) =>
                        names.Select(name => new ObjectSettingEntry { Name = name, ObjectType = objectType, ObjectId = objectId }).ToArray());

                return mock.Object;
            });

            services.AddScoped<IDynamicPropertyUpdaterService>(_ => new Mock<IDynamicPropertyUpdaterService>().Object);
            services.AddScoped<IAggregationConverter>(_ =>
            {
                var mock = new Mock<IAggregationConverter>();
                mock.Setup(x => x.GetAggregationRequestsAsync(It.IsAny<ProductIndexedSearchCriteria>(), It.IsAny<FiltersContainer>()))
                    .ReturnsAsync([]);
                mock.Setup(x => x.ConvertAggregationsAsync(It.IsAny<IList<AggregationResponse>>(), It.IsAny<ProductIndexedSearchCriteria>()))
                    .ReturnsAsync(Array.Empty<Aggregation>());

                return mock.Object;
            });

            // Inventory — every queried product reports fully in stock with a null FulfillmentCenterId.
            services.AddScoped<IInventorySearchService>(_ =>
            {
                var mock = new Mock<IInventorySearchService>();
                mock.Setup(x => x.SearchAsync(It.IsAny<InventorySearchCriteria>(), It.IsAny<bool>()))
                    .ReturnsAsync((InventorySearchCriteria criteria, bool _) =>
                    {
                        var results = (criteria.ProductIds ?? new List<string>())
                            .Select(productId => new InventoryInfo
                            {
                                ProductId = productId,
                                FulfillmentCenterId = null,
                                InStockQuantity = 1_000_000,
                                AllowBackorder = true,
                            })
                            .ToList();

                        return new InventoryInfoSearchResult { Results = results, TotalCount = results.Count };
                    });

                return mock.Object;
            });
            services.AddScoped<IOptionalDependency<IInventorySearchService>>(sp => new TestOptionalDependency<IInventorySearchService>(sp));
            services.AddScoped<IInventoryService>(_ => new Mock<IInventoryService>().Object);
            services.AddScoped<IFulfillmentCenterService>(_ => new Mock<IFulfillmentCenterService>().Object);

            services.AddScoped<IProductIndexedSearchService>(_ => new Mock<IProductIndexedSearchService>().Object);

            // Catalog helper mocks (real impls would require more setup than these tests need).
            services.AddScoped<ISkuGenerator>(_ => new Mock<ISkuGenerator>().Object);
            services.AddScoped<IPropertyValueSanitizer>(_ => new Mock<IPropertyValueSanitizer>().Object);
            services.AddScoped<IOutlinePartNameResolver>(_ =>
            {
                var mock = new Mock<IOutlinePartNameResolver>();
                mock.Setup(x => x.ResolveOutlineName(It.IsAny<Entity>()))
                    .Returns((Entity e) => e switch
                    {
                        CatalogProduct p => p.Name,
                        Category c => c.Name,
                        VirtoCommerce.CatalogModule.Core.Model.Catalog cat => cat.Name,
                        _ => e.Id,
                    });
                mock.Setup(x => x.ResolveLocalizedOutlineName(It.IsAny<Entity>())).Returns((Entity _) => null!);

                return mock.Object;
            });

            // Catalog validators — always pass.
            services.AddScoped<AbstractValidator<IHasProperties>>(_ => CreatePassingValidatorMock<IHasProperties>());
            services.AddScoped<AbstractValidator<CatalogProduct>>(_ => CreatePassingValidatorMock<CatalogProduct>());
            services.AddScoped<AbstractValidator<Property>, PropertyValidator>();
            services.AddScoped<AbstractValidator<IEnumerable<PricelistAssignment>>>(_ => CreatePassingValidatorMock<IEnumerable<PricelistAssignment>>());

            // ---------- Real catalog services (backed by SQLite catalog DB) ----------
            services.AddScoped<IOutlineService, OutlineService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICatalogSearchService, CatalogSearchService>();
            services.AddScoped<ICategorySearchService, CategorySearchService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IPropertyGroupService, PropertyGroupService>();
            services.AddScoped<IPropertyGroupSearchService, PropertyGroupSearchService>();
            services.AddScoped<IPropertySearchService, PropertySearchService>();
            services.AddScoped<IPropertyDictionaryItemService, PropertyDictionaryItemService>();
            services.AddScoped<IPropertyDictionaryItemSearchService, PropertyDictionaryItemSearchService>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IProductSearchService, ProductSearchService>();
            services.AddScoped<IProductConfigurationService, ProductConfigurationService>();
            services.AddScoped<IProductConfigurationSearchService, ProductConfigurationSearchService>();

            // ---------- Real pricing services (backed by SQLite pricing DB) ----------
            services.AddSingleton<ILogger<PricingEvaluatorService>>(NullLogger<PricingEvaluatorService>.Instance);
            services.AddScoped<IPricingPriorityFilterPolicy, DefaultPricingPriorityFilterPolicy>();
            services.AddScoped<IPricingEvaluatorService, PricingEvaluatorService>();
            services.AddScoped<IPricelistService, PricelistService>();
            services.AddScoped<IPricelistSearchService, PricelistSearchService>();
            services.AddScoped<IPriceService, PriceService>();
            services.AddScoped<IPriceSearchService, PriceSearchService>();
            services.AddScoped<IPricelistAssignmentService, PricelistAssignmentService>();
            services.AddScoped<IPricelistAssignmentSearchService, PricelistAssignmentSearchService>();
            services.AddScoped<IOptionalDependency<IPricingEvaluatorService>>(sp =>
            {
                var pricingService = sp.GetRequiredService<IPricingEvaluatorService>();
                var mock = new Mock<IOptionalDependency<IPricingEvaluatorService>>();
                mock.Setup(x => x.Value).Returns(pricingService);
                mock.Setup(x => x.HasValue).Returns(true);

                return mock.Object;
            });

            // ---------- Real store + cart services (backed by SQLite) ----------
            services.AddScoped<IStoreService, StoreService>();
            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddScoped<IShoppingCartSearchService, ShoppingCartSearchService>();
            services.AddScoped<IShoppingCartTotalsCalculator, DefaultShoppingCartTotalsCalculator>();
            // Required by EvalProductsWishlistsMiddleware (registered by AddXCart); only invoked when the
            // LoadWishlists response group is requested, which the cart flow under test does not.
            services.AddScoped<IWishlistService>(_ => new Mock<IWishlistService>().Object);

            // ---------- Search phrase parser + Lucene (in-memory) ----------
            services.AddSingleton<ILogger<SearchPhraseParser>>(NullLogger<SearchPhraseParser>.Instance);
            services.AddScoped<ISearchPhraseParser, SearchPhraseParser>();
            services.AddScoped<IOptionalDependency<ISearchPhraseParser>>(sp => new TestOptionalDependency<ISearchPhraseParser>(sp));
            services.AddSingleton(Options.Create(new LuceneSearchOptions { UseInMemory = true }));
            services.AddSingleton(Options.Create(new SearchOptions { Provider = "Lucene", Scope = "test" }));
            services.AddSingleton<ISearchProvider, LuceneSearchProvider>();

            // ---------- Pipelines ----------
            // AddXCart already registers a SearchProductResponse pipeline with EvalProductsWishlistsMiddleware;
            // these middlewares append to it (named-options accumulate). EvalProductsPricesMiddleware populates
            // ExpProduct.AllPrices (so CartProduct.Price resolves with LoadDependencies=false);
            // EvalProductsInventoryMiddleware populates inventory so tracked products stay orderable.
            services.AddPipeline<SearchProductResponse>(builder =>
            {
                builder.AddMiddleware(typeof(EvalProductsPricesMiddleware));
                builder.AddMiddleware(typeof(EvalProductsInventoryMiddleware));
            });
            services.AddPipeline<IndexSearchRequestBuilder>();
            services.AddPipeline<PriceEvaluationContext>();
            services.AddPipeline<InventorySearchCriteria>();

            // ---------- Auth + user state (always succeed / no-op) ----------
            services.AddScoped<IAuthorizationService>(_ =>
            {
                var mock = new Mock<IAuthorizationService>();
                mock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                    .ReturnsAsync(AuthorizationResult.Success());
                mock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                    .ReturnsAsync(AuthorizationResult.Success());

                return mock.Object;
            });
            services.AddScoped<IUserManagerCore>(_ =>
            {
                var mock = new Mock<IUserManagerCore>();
                mock.Setup(x => x.CheckCurrentUserState(It.IsAny<IResolveFieldContext>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

                return mock.Object;
            });

            // Real LoadUserToEvalContextService (Xapi.Data) — required by CartProductService.
            services.AddTransient<ILoadUserToEvalContextService, VirtoCommerce.Xapi.Data.Services.LoadUserToEvalContextService>();

            return services;
        }

        /// <summary>
        /// Registers the GraphQL infrastructure: scans the Xapi + XCatalog + XCart assemblies for graph
        /// types, schema builders, MediatR handlers and AutoMapper profiles, wires the scoped "cart"
        /// schema and the <see cref="GraphQLTestExecutor"/>. Call after
        /// <see cref="AddXCartComponentTestServices"/>.
        /// <para>
        /// XCatalog's <c>.Data</c> is referenced and its catalog services are registered real, so adding
        /// the XCatalog schema is safe: its schema builders are filtered out of the scoped cart schema but
        /// its MediatR handlers (LoadProductsQueryHandler, SearchProductQueryHandler) get registered — and
        /// the cart-product loading flow needs them.
        /// </para>
        /// </summary>
        public static IServiceCollection AddGraphQLTestServices(this IServiceCollection services)
        {
            var graphQlBuilder = new GraphQLBuilder(services, builder => builder
                .AddNewtonsoftJson()
                .AddSchema(services,
                    typeof(VirtoCommerce.Xapi.Core.CoreAssemblyMarker),
                    typeof(VirtoCommerce.Xapi.Data.DataAssemblyMarker))
                .AddSchema(services,
                    typeof(VirtoCommerce.XCatalog.Core.CoreAssemblyMarker),
                    typeof(VirtoCommerce.XCatalog.Data.DataAssemblyMarker))
                .AddSchema(services,
                    typeof(VirtoCommerce.XCart.Core.CoreAssemblyMarker),
                    typeof(VirtoCommerce.XCart.Data.DataAssemblyMarker))
                .AddDataLoader());

            // Real X-Cart services (CartAggregate, builders, repositories, pipelines, ...).
            services.AddXCart(graphQlBuilder);

            // PurchaseSchema depends on IDistributedLockService; tests need no real locking.
            services.AddSingleton<IDistributedLockService, NoOpDistributedLockService>();

            // Schema initialization resolves the ctor dependencies of EVERY registered GraphQL type across
            // all scanned assemblies, so each must be registrable. These are only needed for type construction.
            services.AddScoped<IMeasureService>(_ => new Mock<IMeasureService>().Object);
            services.AddScoped<IDynamicPropertyResolverService>(_ => new Mock<IDynamicPropertyResolverService>().Object);
            services.AddScoped<IDynamicPropertyDictionaryItemsService>(_ => new Mock<IDynamicPropertyDictionaryItemsService>().Object);
            services.AddScoped<ILocalizableSettingService>(_ => new Mock<ILocalizableSettingService>().Object);
            services.AddScoped<IPickupLocationSearchService>(_ => new Mock<IPickupLocationSearchService>().Object);
            services.AddScoped<IPickupLocationService>(_ => new Mock<IPickupLocationService>().Object);
            services.AddScoped<IOptionalDependency<IFulfillmentCenterGeoService>>(_ =>
            {
                var mock = new Mock<IOptionalDependency<IFulfillmentCenterGeoService>>();
                mock.Setup(x => x.HasValue).Returns(false);

                return mock.Object;
            });

            // The scoped "cart" schema only builds the X-Cart schema builders.
            services.AddSingleton<ISchemaFilter, CustomSchemaFilter>();
            services.AddSingleton<ScopedSchemaFactory<VirtoCommerce.XCart.Data.DataAssemblyMarker>>();
            services.AddSingleton<ISchema>(sp => sp.GetRequiredService<ScopedSchemaFactory<VirtoCommerce.XCart.Data.DataAssemblyMarker>>());

            services.AddScoped<GraphQLTestExecutor>();

            return services;
        }

        private static AbstractValidator<T> CreatePassingValidatorMock<T>()
        {
            var mock = new Mock<AbstractValidator<T>>();
            mock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<T>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            return mock.Object;
        }

        /// <summary>
        /// Lazily resolves <typeparamref name="T"/> from the service provider. Mirrors the platform's
        /// OptionalDependencyManager without taking a dependency on the Platform.Modules assembly.
        /// </summary>
        private sealed class TestOptionalDependency<T>(IServiceProvider serviceProvider) : IOptionalDependency<T>
        {
            public bool HasValue => true;

            public T Value => serviceProvider.GetRequiredService<T>();
        }

        /// <summary>No-op distributed lock — runs the resolver directly (no locking in tests).</summary>
        private sealed class NoOpDistributedLockService : IDistributedLockService
        {
            public T Execute<T>(string resourceKey, Func<T> resolver) => resolver();

            public Task<T> ExecuteAsync<T>(string resourceKey, Func<Task<T>> resolver) => resolver();
        }
    }
}
