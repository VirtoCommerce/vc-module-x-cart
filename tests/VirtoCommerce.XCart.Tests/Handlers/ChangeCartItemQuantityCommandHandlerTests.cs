using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class ChangeCartItemQuantityCommandHandlerTests
    {
        private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
        private readonly Mock<ICartProductService> _cartProductServiceMock = new();

        private ChangeCartItemQuantityCommandHandler CreateHandler()
        {
            return new ChangeCartItemQuantityCommandHandler(
                _cartAggregateRepositoryMock.Object,
                _cartProductServiceMock.Object);
        }

        private Mock<CartAggregate> SetupCartAggregate(LineItem lineItem)
        {
            var mock = new Mock<CartAggregate>(
                MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null, null);

            // Cart property has a protected setter and is non-virtual, so we set it via reflection
            var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
            cartProperty.SetValue(mock.Object, new ShoppingCart { Id = "cart-1", Items = new List<LineItem> { lineItem } });

            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync(new Dictionary<string, CartProduct>
                {
                    { CartAggregate.FormatGetCartProductKey(lineItem.ProductId, lineItem.Currency), new CartProduct(new CatalogProduct { Id = lineItem.ProductId }) },
                });

            return mock;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task Handle_NonPositiveQuantity_RemovesLineItem(int quantity)
        {
            // Arrange
            var lineItem = new LineItem { Id = "li-1", ProductId = "prod-1", Currency = "USD", Quantity = 5 };
            var cartAggregateMock = SetupCartAggregate(lineItem);

            var request = new ChangeCartItemQuantityCommand
            {
                CartId = "cart-1",
                LineItemId = "li-1",
                Quantity = quantity,
            };

            // Act
            await CreateHandler().Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.RemoveItemAsync("li-1"), Times.Once);
            cartAggregateMock.Verify(x => x.ChangeItemQuantityAsync(It.IsAny<ItemQtyAdjustment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_PositiveQuantity_ChangesQuantity()
        {
            // Arrange
            var lineItem = new LineItem { Id = "li-1", ProductId = "prod-1", Currency = "USD", Quantity = 5 };
            var cartAggregateMock = SetupCartAggregate(lineItem);

            var request = new ChangeCartItemQuantityCommand
            {
                CartId = "cart-1",
                LineItemId = "li-1",
                Quantity = 3,
            };

            // Act
            await CreateHandler().Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.RemoveItemAsync(It.IsAny<string>()), Times.Never);
            cartAggregateMock.Verify(x => x.ChangeItemQuantityAsync(
                It.Is<ItemQtyAdjustment>(a => a.LineItemId == "li-1" && a.NewQuantity == 3)), Times.Once);
        }
    }
}
