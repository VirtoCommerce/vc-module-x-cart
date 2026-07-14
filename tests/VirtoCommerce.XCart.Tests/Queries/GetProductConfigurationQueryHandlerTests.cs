using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Queries;
using Xunit;
using CatalogProductConfigurationSection = VirtoCommerce.CatalogModule.Core.Model.Configuration.ProductConfigurationSection;

namespace VirtoCommerce.XCart.Tests.Queries
{
    public class GetProductConfigurationQueryHandlerTests
    {
        private readonly Mock<IProductConfigurationSearchService> _searchServiceMock = new(MockBehavior.Strict);
        private readonly Mock<IConfiguredLineItemContainerService> _containerServiceMock = new(MockBehavior.Strict);
        private readonly Mock<ICartProductsLoaderService> _cartProductServiceMock = new(MockBehavior.Strict);

        /// <summary>
        /// Exposes the protected <c>GetResponseGroup</c> and bypasses the search service by returning a canned configuration,
        /// so the option-loading branch can be exercised without wiring the catalog search.
        /// </summary>
        private sealed class TestableHandler : GetProductConfigurationQueryHandler
        {
            private readonly ProductConfiguration _configuration;

            public TestableHandler(
                IProductConfigurationSearchService searchService,
                IConfiguredLineItemContainerService containerService,
                ICartProductsLoaderService cartProductService,
                ProductConfiguration configuration)
                : base(searchService, containerService, cartProductService)
            {
                _configuration = configuration;
            }

            public ProductConfigurationResponseGroup PublicGetResponseGroup(GetProductConfigurationQuery request) => GetResponseGroup(request);

            protected override Task<ProductConfiguration> GetConfiguration(GetProductConfigurationQuery request, ProductConfigurationResponseGroup responseGroup)
                => Task.FromResult(_configuration);
        }

        private TestableHandler CreateHandler(ProductConfiguration configuration = null)
            => new(_searchServiceMock.Object, _containerServiceMock.Object, _cartProductServiceMock.Object, configuration);

        [Theory]
        // No field selection (e.g. the standalone product configuration query) -> load the full graph.
        [InlineData(new string[0], ProductConfigurationResponseGroup.Full)]
        // Only section metadata -> sections only.
        [InlineData(new[] { "id", "name", "type" }, ProductConfigurationResponseGroup.Sections)]
        // Options requested without nested product/price -> sections + options.
        [InlineData(new[] { "id", "options", "options.id", "options.text" }, ProductConfigurationResponseGroup.Options)]
        // Options with a nested product -> full graph.
        [InlineData(new[] { "options", "options.product.id" }, ProductConfigurationResponseGroup.Full)]
        // Options with a nested price -> full graph.
        [InlineData(new[] { "options", "options.salePrice.amount" }, ProductConfigurationResponseGroup.Full)]
        public void GetResponseGroup_MapsIncludeFieldsToResponseGroup(string[] includeFields, ProductConfigurationResponseGroup expected)
        {
            // Arrange
            var handler = CreateHandler();
            var request = new GetProductConfigurationQuery { IncludeFields = includeFields };

            // Act
            var responseGroup = handler.PublicGetResponseGroup(request);

            // Assert
            responseGroup.Should().Be(expected);
        }

        [Fact]
        public async Task Handle_SectionsOnly_DoesNotLoadOptionProducts()
        {
            // Arrange
            var configuration = new ProductConfiguration
            {
                ProductId = "configurable-1",
                Sections =
                [
                    new CatalogProductConfigurationSection { Id = "section-1", Name = "Engraving", Type = "Text", DisplayOrder = 0 },
                ],
            };

            var handler = CreateHandler(configuration);

            // No "options" path requested -> Sections response group -> the option/product enrichment must be skipped.
            var request = new GetProductConfigurationQuery
            {
                ConfigurableProductId = "configurable-1",
                IncludeFields = ["id", "name", "type"],
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.ConfigurationSections.Should().ContainSingle();
            result.ConfigurationSections[0].Id.Should().Be("section-1");
            result.ConfigurationSections[0].Name.Should().Be("Engraving");
            result.ConfigurationSections[0].Options.Should().BeEmpty();

            // Strict mocks would throw if these were called; the explicit verification documents the intent.
            _containerServiceMock.Verify(x => x.CreateContainerAsync(It.IsAny<ICartProductContainerRequest>()), Times.Never);
            _cartProductServiceMock.Verify(x => x.GetCartProductsAsync(It.IsAny<CartProductsRequest>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithSectionIds_BuildsOnlyRequestedSections()
        {
            // Arrange
            var configuration = new ProductConfiguration
            {
                ProductId = "configurable-1",
                Sections =
                [
                    new CatalogProductConfigurationSection { Id = "section-A", Name = "A", Type = "Text", DisplayOrder = 0 },
                    new CatalogProductConfigurationSection { Id = "section-B", Name = "B", Type = "Text", DisplayOrder = 1 },
                    new CatalogProductConfigurationSection { Id = "section-C", Name = "C", Type = "Text", DisplayOrder = 2 },
                ],
            };

            var handler = CreateHandler(configuration);

            // Only section-A requested -> only that section is built; B and C are skipped.
            var request = new GetProductConfigurationQuery
            {
                ConfigurableProductId = "configurable-1",
                SectionIds = ["section-A"],
                IncludeFields = ["id", "name"],
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.ConfigurationSections.Should().ContainSingle();
            result.ConfigurationSections[0].Id.Should().Be("section-A");
        }
    }
}
