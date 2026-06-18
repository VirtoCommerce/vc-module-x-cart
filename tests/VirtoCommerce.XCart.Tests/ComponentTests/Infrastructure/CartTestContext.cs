using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CartModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Data.Model;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.StoreModule.Data.Model;
using VirtoCommerce.StoreModule.Data.Repositories;
using VirtoCommerce.XCart.Tests.ComponentTests.Helpers;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Provides an isolated DI container for an X-Cart GraphQL component test. Each test builds its own
    /// <see cref="CartTestContext"/> with its own in-memory SQLite databases (cart, catalog, pricing,
    /// store), real catalog/cart/pricing/store services, a real in-memory Lucene index and the full
    /// GraphQL pipeline. Dispose after the test.
    /// </summary>
    internal sealed class CartTestContext : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _cartConnection;
        private readonly SqliteConnection _catalogConnection;
        private readonly SqliteConnection _pricingConnection;
        private readonly SqliteConnection _storeConnection;
        private readonly IList<string> _seededProductIds;

        private CartTestContext(
            ServiceProvider serviceProvider,
            IServiceScope scope,
            SqliteConnection cartConnection,
            SqliteConnection catalogConnection,
            SqliteConnection pricingConnection,
            SqliteConnection storeConnection,
            IList<string> seededProductIds)
        {
            _serviceProvider = serviceProvider;
            _scope = scope;
            _cartConnection = cartConnection;
            _catalogConnection = catalogConnection;
            _pricingConnection = pricingConnection;
            _storeConnection = storeConnection;
            _seededProductIds = seededProductIds;
        }

        public static CartTestContextBuilder Create() => new();

        /// <summary>Resolve a service from the test's DI scope.</summary>
        public T GetRequiredService<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

        /// <summary>Execute a GraphQL query/mutation through the full pipeline.</summary>
        public Task<GraphQLTestResult> ExecuteAsync(
            string query,
            Dictionary<string, object>? variables = null,
            string userId = "test-user",
            string? userName = null)
        {
            return GetRequiredService<GraphQLTestExecutor>().ExecuteAsync(query, userId: userId, userName: userName, variables: variables);
        }

        /// <summary>
        /// Builds the in-memory Lucene index for the seeded catalog products. Must be called after
        /// <see cref="CartTestContextBuilder.Build"/> for tests that resolve products through search.
        /// </summary>
        public async Task CreateIndexAsync()
        {
            if (_seededProductIds.Count == 0)
            {
                return;
            }

            var itemService = GetRequiredService<IItemService>();
            var productSearchService = GetRequiredService<IProductSearchService>();
            var searchProvider = GetRequiredService<ISearchProvider>();

            var documentBuilder = ProductIndexingTestHelper.CreateProductDocumentBuilder(itemService, productSearchService);
            await ProductIndexingTestHelper.IndexProductsAsync(searchProvider, documentBuilder, _seededProductIds);
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _serviceProvider?.Dispose();
            _cartConnection?.Dispose();
            _catalogConnection?.Dispose();
            _pricingConnection?.Dispose();
            _storeConnection?.Dispose();
        }

        /// <summary>Fluent builder for <see cref="CartTestContext"/>.</summary>
        internal sealed class CartTestContextBuilder
        {
            private readonly List<StoreEntity> _storesToSeed = [];
            private readonly List<CatalogEntity> _catalogsToSeed = [];
            private readonly List<CategoryEntity> _categoriesToSeed = [];
            private readonly List<ItemEntity> _productsToSeed = [];
            private readonly List<string[]> _defaultPricingToSeed = [];
            private readonly List<(string ProductId, decimal ListPrice, decimal? SalePrice)[]> _customPricingToSeed = [];

            public CartTestContextBuilder SeedStores(params StoreEntity[] stores)
            {
                _storesToSeed.AddRange(stores);

                return this;
            }

            public CartTestContextBuilder SeedCatalogs(params CatalogEntity[] catalogs)
            {
                _catalogsToSeed.AddRange(catalogs);

                return this;
            }

            public CartTestContextBuilder SeedCategories(params CategoryEntity[] categories)
            {
                _categoriesToSeed.AddRange(categories);

                return this;
            }

            public CartTestContextBuilder SeedProducts(params ItemEntity[] products)
            {
                _productsToSeed.AddRange(products);

                return this;
            }

            public CartTestContextBuilder SeedDefaultPricing(params string[] productIds)
            {
                _defaultPricingToSeed.Add(productIds);

                return this;
            }

            public CartTestContextBuilder SeedPricing(params (string ProductId, decimal ListPrice, decimal? SalePrice)[] prices)
            {
                _customPricingToSeed.Add(prices);

                return this;
            }

            public CartTestContext Build()
            {
                var cartConnection = SqliteTestDbContextFactory.CreateConnection();
                var catalogConnection = SqliteTestDbContextFactory.CreateConnection();
                var pricingConnection = SqliteTestDbContextFactory.CreateConnection();
                var storeConnection = SqliteTestDbContextFactory.CreateConnection();

                var cartDbOptions = SqliteTestDbContextFactory.CreateDbContextOptions<CartDbContext>(cartConnection);
                var catalogDbOptions = SqliteTestDbContextFactory.CreateDbContextOptions<CatalogDbContext>(catalogConnection);
                var pricingDbOptions = SqliteTestDbContextFactory.CreateDbContextOptions<PricingDbContext>(pricingConnection);
                var storeDbOptions = SqliteTestDbContextFactory.CreateDbContextOptions<StoreDbContext>(storeConnection);

                var services = new ServiceCollection();

                // Per-test singletons: the DbContext options bound to this test's open connections.
                services.AddSingleton(cartDbOptions);
                services.AddSingleton(catalogDbOptions);
                services.AddSingleton(pricingDbOptions);
                services.AddSingleton(storeDbOptions);

                services.AddXCartComponentTestServices();
                services.AddGraphQLTestServices();

                var serviceProvider = services.BuildServiceProvider();
                var scope = serviceProvider.CreateScope();

                var seededProductIds = _productsToSeed.Select(x => x.Id).ToList();

                var context = new CartTestContext(
                    serviceProvider,
                    scope,
                    cartConnection,
                    catalogConnection,
                    pricingConnection,
                    storeConnection,
                    seededProductIds);

                // Seed store data.
                if (_storesToSeed.Count > 0)
                {
                    using var storeDb = new StoreDbContext(storeDbOptions);
                    storeDb.AddRange(_storesToSeed);
                    storeDb.SaveChanges();
                }

                // Seed catalog data in order: catalogs -> categories -> products.
                if (_catalogsToSeed.Count > 0 || _categoriesToSeed.Count > 0 || _productsToSeed.Count > 0)
                {
                    using var catalogDb = new CatalogDbContext(catalogDbOptions);

                    if (_catalogsToSeed.Count > 0)
                    {
                        catalogDb.AddRange(_catalogsToSeed);
                        catalogDb.SaveChanges();
                    }

                    if (_categoriesToSeed.Count > 0)
                    {
                        catalogDb.AddRange(_categoriesToSeed);
                        catalogDb.SaveChanges();
                    }

                    if (_productsToSeed.Count > 0)
                    {
                        catalogDb.AddRange(_productsToSeed);
                        catalogDb.SaveChanges();
                    }
                }

                // Seed pricing data.
                if (_defaultPricingToSeed.Count > 0 || _customPricingToSeed.Count > 0)
                {
                    using var pricingDb = new PricingDbContext(pricingDbOptions);

                    foreach (var productIds in _defaultPricingToSeed)
                    {
                        PricingDataSeeder.SeedDefaultPricing(pricingDb, productIds);
                    }

                    foreach (var prices in _customPricingToSeed)
                    {
                        PricingDataSeeder.SeedPricing(pricingDb, prices);
                    }
                }

                return context;
            }
        }
    }
}
