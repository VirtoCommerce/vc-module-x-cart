using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CatalogModule.Data.Search.Indexing;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Helpers
{
    /// <summary>
    /// Helper for setting up product indexing with an in-memory Lucene search provider for component tests.
    /// This enables realistic testing of the cart-product loading flow (which resolves products through the
    /// search index) without external dependencies.
    /// </summary>
    internal static class ProductIndexingTestHelper
    {
        /// <summary>
        /// Creates a <see cref="ProductDocumentBuilder"/> that stores full product objects in the index
        /// (so <c>ExpProduct.IndexedProduct</c> / the <c>__object</c> field is populated).
        /// </summary>
        public static ProductDocumentBuilder CreateProductDocumentBuilder(
            IItemService itemService,
            IProductSearchService productSearchService)
        {
            var settingsManagerMock = new Mock<ISettingsManager>();
            var propertySearchServiceMock = new Mock<IPropertySearchService>();
            var measureServiceMock = new Mock<IMeasureService>();

            // Return null for settings so the builder uses defaults.
            settingsManagerMock
                .Setup(x => x.GetObjectSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((ObjectSettingEntry?)null);

            return new TestableProductDocumentBuilder(
                settingsManagerMock.Object,
                propertySearchServiceMock.Object,
                itemService,
                productSearchService,
                measureServiceMock.Object);
        }

        /// <summary>
        /// Indexes products into the search provider using the document builder.
        /// </summary>
        public static async Task<IndexingResult> IndexProductsAsync(
            ISearchProvider searchProvider,
            IIndexDocumentBuilder documentBuilder,
            IList<string> productIds)
        {
            var documents = await documentBuilder.GetDocumentsAsync(productIds, CancellationToken.None);

            return await searchProvider.IndexAsync(KnownDocumentTypes.Product, documents);
        }

        /// <summary>
        /// Testable subclass of the base <see cref="ProductDocumentBuilder"/> that forces full-object
        /// storage in the index so <c>ExpProduct.IndexedProduct</c> resolves without a database round-trip.
        /// </summary>
        private sealed class TestableProductDocumentBuilder : ProductDocumentBuilder
        {
            public TestableProductDocumentBuilder(
                ISettingsManager settingsManager,
                IPropertySearchService propertySearchService,
                IItemService itemService,
                IProductSearchService productsSearchService,
                IMeasureService measureService)
                : base(settingsManager, propertySearchService, itemService, productsSearchService, measureService)
            {
            }

            protected override bool StoreObjectsInIndex => true;
        }
    }
}
