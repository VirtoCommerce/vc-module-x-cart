using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class AddCartItemCommandHandlerTests
    {
        private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
        private readonly Mock<ICartProductService> _cartProductServiceMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IProductConfigurationSearchService> _configSearchServiceMock = new();

        private AddCartItemCommandHandler CreateHandler()
        {
            return new AddCartItemCommandHandler(
                _cartAggregateRepositoryMock.Object,
                _cartProductServiceMock.Object,
                _mediatorMock.Object,
                _configSearchServiceMock.Object);
        }

        private static Mock<CartAggregate> CreateCartAggregateMock()
        {
            var mock = new Mock<CartAggregate>(
                MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null);

            // Cart property has a protected setter and is non-virtual, so we set it via reflection
            var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
            cartProperty.SetValue(mock.Object, new ShoppingCart { Id = "cart-1" });

            return mock;
        }

        [Fact]
        public async Task Handle_NonConfigurableProduct_CallsAddItemAsync()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            var product = new CartProduct(new CatalogProduct { Id = "prod-1", IsActive = true, IsBuyable = true });
            _cartProductServiceMock
                .Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), It.Is<IList<string>>(ids => ids.Contains("prod-1"))))
                .ReturnsAsync(new List<CartProduct> { product });

            // No active configuration
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult());

            var handler = CreateHandler();
            var request = new AddCartItemCommand
            {
                CartId = "cart-1",
                ProductId = "prod-1",
                Quantity = 2,
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.AddItemAsync(It.Is<NewCartItem>(i => i.ProductId == "prod-1" && i.Quantity == 2)), Times.Once);
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(It.IsAny<NewCartItem>(), It.IsAny<LineItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ConfigurableProduct_DispatchesCreateConfiguredLineItemCommand()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            var product = new CartProduct(new CatalogProduct { Id = "prod-1", IsActive = true, IsBuyable = true });
            _cartProductServiceMock
                .Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<string>>()))
                .ReturnsAsync(new List<CartProduct> { product });

            // Active configuration exists
            var config = new ProductConfiguration { ProductId = "prod-1", IsActive = true };
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult { Results = [config], TotalCount = 1 });

            var configuredLineItem = new LineItem { ProductId = "prod-1" };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpConfigurationLineItem { Item = configuredLineItem });

            var handler = CreateHandler();
            var request = new AddCartItemCommand
            {
                CartId = "cart-1",
                StoreId = "store-1",
                UserId = "user-1",
                ProductId = "prod-1",
                Quantity = 1,
                ConfigurationSections = [],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(x => x.Send(
                It.Is<CreateConfiguredLineItemCommand>(c =>
                    c.ConfigurableProductId == "prod-1" &&
                    c.StoreId == "store-1" &&
                    c.CartId == "cart-1"),
                It.IsAny<CancellationToken>()), Times.Once);
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(
                It.Is<NewCartItem>(i => i.ProductId == "prod-1"),
                It.Is<LineItem>(li => li.ProductId == "prod-1")), Times.Once);
            cartAggregateMock.Verify(x => x.AddItemAsync(It.IsAny<NewCartItem>()), Times.Never);
        }
    }
}
