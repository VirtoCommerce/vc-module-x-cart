using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class AddCartItemsCommandHandlerTests
    {
        private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
        private readonly Mock<ICartProductService> _cartProductServiceMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IProductConfigurationSearchService> _configSearchServiceMock = new();

        private AddCartItemsCommandHandler CreateHandler()
        {
            return new AddCartItemsCommandHandler(
                _cartAggregateRepositoryMock.Object,
                _cartProductServiceMock.Object,
                _mediatorMock.Object,
                _configSearchServiceMock.Object);
        }

        private static Mock<CartAggregate> CreateCartAggregateMock()
        {
            var mock = new Mock<CartAggregate>(
                MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null, null);

            // Cart property has a protected setter and is non-virtual, so we set it via reflection
            var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
            cartProperty.SetValue(mock.Object, new ShoppingCart { Id = "cart-1" });

            return mock;
        }

        [Fact]
        public async Task Handle_NonConfigurableProducts_CallsAddItemAsyncPerItem()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync((CartAggregate _, IList<(string CurrencyCode, string ProductId)> productPairs) =>
                {
                    var cartProduct1 = new CartProduct(new CatalogProduct { Id = "prod-1" });
                    var cartProduct2 = new CartProduct(new CatalogProduct { Id = "prod-2" });

                    return new Dictionary<string, CartProduct>
                    {
                        { CartAggregate.GetCartProductKey(cartProduct1.Id, "USD"), cartProduct1 },
                        { CartAggregate.GetCartProductKey(cartProduct2.Id, "USD"), cartProduct2 },
                    };
                });

            // No configurations
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult());

            var handler = CreateHandler();
            var request = new AddCartItemsCommand
            {
                CartId = "cart-1",
                CartItems = [new("prod-1", 1) { ItemCurrencyCode = "USD" }, new("prod-2", 2) { ItemCurrencyCode = "USD" }],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.AddItemAsync(It.IsAny<NewCartItem>()), Times.Exactly(2));
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(It.IsAny<NewCartItem>(), It.IsAny<LineItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ConfigurableProducts_CallsAddConfiguredItemAsync()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();

            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync((CartAggregate _, IList<(string CurrencyCode, string ProductId)> productPairs) =>
                {
                    var cartProduct = new CartProduct(new CatalogProduct { Id = "prod-1", });

                    return new Dictionary<string, CartProduct>
                    {
                        { CartAggregate.GetCartProductKey(cartProduct.Id, "USD"), cartProduct },
                    };
                });

            var config = new ProductConfiguration { ProductId = "prod-1", IsActive = true };
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult { Results = [config], TotalCount = 1 });

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpConfigurationLineItem { Item = new LineItem { ProductId = "prod-1" } });

            var handler = CreateHandler();
            var request = new AddCartItemsCommand
            {
                CartId = "cart-1",
                StoreId = "store-1",
                UserId = "user-1",
                CartItems = [new("prod-1", 1) { ItemCurrencyCode = "USD" }],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(x => x.Send(
                It.Is<CreateConfiguredLineItemCommand>(c => c.ConfigurableProductId == "prod-1"),
                It.IsAny<CancellationToken>()), Times.Once);
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(It.IsAny<NewCartItem>(), It.IsAny<LineItem>()), Times.Once);
            cartAggregateMock.Verify(x => x.AddItemAsync(It.IsAny<NewCartItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MixedBatch_RoutesCorrectly()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync((CartAggregate _, IList<(string CurrencyCode, string ProductId)> productPairs) =>
                {
                    var cartProduct1 = new CartProduct(new CatalogProduct { Id = "configurable-1" });
                    var cartProduct2 = new CartProduct(new CatalogProduct { Id = "regular-1" });

                    return new Dictionary<string, CartProduct>
                    {
                        { CartAggregate.GetCartProductKey(cartProduct1.Id, "USD"), cartProduct1 },
                        { CartAggregate.GetCartProductKey(cartProduct2.Id, "USD"), cartProduct2 },
                    };
                });

            var config = new ProductConfiguration { ProductId = "configurable-1", IsActive = true };
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult { Results = [config], TotalCount = 1 });

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpConfigurationLineItem { Item = new LineItem { ProductId = "configurable-1" } });

            var handler = CreateHandler();
            var request = new AddCartItemsCommand
            {
                CartId = "cart-1",
                StoreId = "store-1",
                CartItems = [new("configurable-1", 1) { ItemCurrencyCode = "USD" }, new("regular-1", 2) { ItemCurrencyCode = "USD" }],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(It.IsAny<NewCartItem>(), It.IsAny<LineItem>()), Times.Once);
            cartAggregateMock.Verify(x => x.AddItemAsync(It.IsAny<NewCartItem>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ProductNotFound_AddsValidationError()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync(new Dictionary<string, CartProduct>()); // no products found

            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult());

            var handler = CreateHandler();
            var request = new AddCartItemsCommand
            {
                CartId = "cart-1",
                CartItems = [new("missing-prod", 1) { ItemCurrencyCode = "USD" }],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Contains(cartAggregateMock.Object.OperationValidationErrors,
                x => x.ErrorCode == "CART_PRODUCT_UNAVAILABLE");
            cartAggregateMock.Verify(x => x.AddItemAsync(It.IsAny<NewCartItem>()), Times.Never);
            cartAggregateMock.Verify(x => x.AddConfiguredItemAsync(It.IsAny<NewCartItem>(), It.IsAny<LineItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ConfigurableWithoutSections_PassesNullConfigurationSections()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync((CartAggregate _, IList<(string CurrencyCode, string ProductId)> productPairs) =>
                {
                    var cartProduct = new CartProduct(new CatalogProduct { Id = "prod-1", });

                    return new Dictionary<string, CartProduct>
                    {
                        { CartAggregate.GetCartProductKey(cartProduct.Id, "USD"), cartProduct },
                    };
                });

            var config = new ProductConfiguration { ProductId = "prod-1", IsActive = true };
            _configSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult { Results = [config], TotalCount = 1 });

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpConfigurationLineItem { Item = new LineItem { ProductId = "prod-1" } });

            var handler = CreateHandler();
            var request = new AddCartItemsCommand
            {
                CartId = "cart-1",
                StoreId = "store-1",
                CartItems = [new("prod-1", 1) { ItemCurrencyCode = "USD" }], // no ConfigurationSections set
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert — configurable product still routes through configured path even without sections
            _mediatorMock.Verify(x => x.Send(
                It.Is<CreateConfiguredLineItemCommand>(c =>
                    c.ConfigurableProductId == "prod-1" &&
                    c.ConfigurationSections == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
