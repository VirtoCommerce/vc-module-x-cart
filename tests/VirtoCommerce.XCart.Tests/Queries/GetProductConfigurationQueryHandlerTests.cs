using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Data.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Queries;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;
using CatalogProductConfigurationSection = VirtoCommerce.CatalogModule.Core.Model.Configuration.ProductConfigurationSection;

namespace VirtoCommerce.XCart.Tests.Queries
{
    public class GetProductConfigurationQueryHandlerTests
    {
        private readonly Mock<IProductConfigurationSearchService> _searchServiceMock = new(MockBehavior.Strict);
        private readonly Mock<IConfiguredLineItemContainerService> _containerServiceMock = new(MockBehavior.Strict);
        private readonly Mock<ICartProductsLoaderService> _cartProductServiceMock = new(MockBehavior.Strict);
        // Real caching implementation, shared across every handler this fixture builds — so two sends requesting
        // the same option product ids dedup the load per-id (see Handle_SharedRequestCache_* below).
        private readonly IRequestScopedCache _requestScopedCache = new RequestScopedCache();

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
                IRequestScopedCache requestScopedCache,
                ProductConfiguration configuration)
                : base(searchService, containerService, cartProductService, requestScopedCache)
            {
                _configuration = configuration;
            }

            public ProductConfigurationResponseGroup PublicGetResponseGroup(GetProductConfigurationQuery request) => GetResponseGroup(request);

            protected override Task<ProductConfiguration> GetConfiguration(GetProductConfigurationQuery request, ProductConfigurationResponseGroup responseGroup)
                => Task.FromResult(_configuration);
        }

        private TestableHandler CreateHandler(ProductConfiguration configuration = null)
            => new(_searchServiceMock.Object, _containerServiceMock.Object, _cartProductServiceMock.Object, _requestScopedCache, configuration);

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

        [Fact]
        public async Task Handle_SharedRequestCache_DedupsSameKeyAndReloadsOnDifferentKey()
        {
            // Arrange
            _containerServiceMock
                .Setup(x => x.CreateContainerAsync(It.IsAny<ICartProductContainerRequest>()))
                .ReturnsAsync(new ConfiguredLineItemContainer { CultureName = "en-US" });

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartProductsRequest>()))
                .ReturnsAsync(new List<CartProduct>());

            var handler = CreateHandler(BuildProductConfiguration(optionProductId: "opt-1"));
            var request = new GetProductConfigurationQuery
            {
                ConfigurableProductId = "configurable-1",
                IncludeFields = ["options", "options.product.id"],
            };

            // Act — two sends against the same handler (same shared IRequestScopedCache instance) request the
            // same option product id under the same prefix (store/currency/culture/etc), so the by-id cache dedups.
            await handler.Handle(request, CancellationToken.None);
            await handler.Handle(request, CancellationToken.None);

            // Assert — the second send dedups against the first via the shared request cache.
            _cartProductServiceMock.Verify(x => x.GetCartProductsAsync(It.IsAny<CartProductsRequest>()), Times.Once);

            // Act — a third send, from a handler sharing the same request cache but requesting a DIFFERENT
            // option product id (not yet cached).
            var differingHandler = CreateHandler(BuildProductConfiguration(optionProductId: "opt-2"));
            var differingRequest = new GetProductConfigurationQuery
            {
                ConfigurableProductId = "configurable-2",
                IncludeFields = ["options", "options.product.id"],
            };

            await differingHandler.Handle(differingRequest, CancellationToken.None);

            // Assert — the new product id is not cached, so the loader runs again for it.
            _cartProductServiceMock.Verify(x => x.GetCartProductsAsync(It.IsAny<CartProductsRequest>()), Times.Exactly(2));
        }

        private static ProductConfiguration BuildProductConfiguration(string optionProductId) => new()
        {
            ProductId = "configurable-1",
            Sections =
            [
                new CatalogProductConfigurationSection
                {
                    Id = "section-1",
                    Name = "Add-ons",
                    Type = ConfigurationSectionTypeProduct,
                    DisplayOrder = 0,
                    Options =
                    [
                        new ProductConfigurationOption { Id = "option-1", ProductId = optionProductId, Quantity = 1 },
                    ],
                },
            ],
        };
    }
}
