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
    public class UpdateWishlistItemsCommandHandlerTests
    {
        private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
        private readonly Mock<ICartProductService> _cartProductServiceMock = new();

        private UpdateWishlistItemsCommandHandler CreateHandler()
        {
            return new UpdateWishlistItemsCommandHandler(
                _cartAggregateRepositoryMock.Object,
                _cartProductServiceMock.Object);
        }

        private Mock<CartAggregate> SetupWishlistAggregate(params LineItem[] lineItems)
        {
            var mock = new Mock<CartAggregate>(
                MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null, null);

            // Cart property has a protected setter and is non-virtual, so we set it via reflection
            var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
            cartProperty.SetValue(mock.Object, new ShoppingCart { Id = "list-1", Items = new List<LineItem>(lineItems) });

            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mock.Object);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync((CartAggregate _, IList<(string CurrencyCode, string ProductId)> pairs) =>
                {
                    var result = new Dictionary<string, CartProduct>();
                    foreach (var (currencyCode, productId) in pairs)
                    {
                        result[CartAggregate.FormatGetCartProductKey(productId, currencyCode)] =
                            new CartProduct(new CatalogProduct { Id = productId });
                    }
                    return result;
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
            var wishlistAggregateMock = SetupWishlistAggregate(lineItem);

            var request = new UpdateWishlistItemsCommand
            {
                ListId = "list-1",
                Items = [new WishListItem { LineItemId = "li-1", Quantity = quantity }],
            };

            // Act
            await CreateHandler().Handle(request, CancellationToken.None);

            // Assert
            wishlistAggregateMock.Verify(x => x.RemoveItemAsync("li-1"), Times.Once);
            wishlistAggregateMock.Verify(x => x.ChangeItemQuantityAsync(It.IsAny<ItemQtyAdjustment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MixedQuantities_RemovesNegativeAndUpdatesPositive()
        {
            // Arrange
            var lineItem1 = new LineItem { Id = "li-1", ProductId = "prod-1", Currency = "USD", Quantity = 5 };
            var lineItem2 = new LineItem { Id = "li-2", ProductId = "prod-2", Currency = "USD", Quantity = 1 };
            var wishlistAggregateMock = SetupWishlistAggregate(lineItem1, lineItem2);

            var request = new UpdateWishlistItemsCommand
            {
                ListId = "list-1",
                Items =
                [
                    new WishListItem { LineItemId = "li-1", Quantity = -1 },
                    new WishListItem { LineItemId = "li-2", Quantity = 3 },
                ],
            };

            // Act
            await CreateHandler().Handle(request, CancellationToken.None);

            // Assert
            wishlistAggregateMock.Verify(x => x.RemoveItemAsync("li-1"), Times.Once);
            wishlistAggregateMock.Verify(x => x.RemoveItemAsync("li-2"), Times.Never);
            wishlistAggregateMock.Verify(x => x.ChangeItemQuantityAsync(
                It.Is<ItemQtyAdjustment>(a => a.LineItemId == "li-2" && a.NewQuantity == 3)), Times.Once);
            wishlistAggregateMock.Verify(x => x.ChangeItemQuantityAsync(
                It.Is<ItemQtyAdjustment>(a => a.LineItemId == "li-1")), Times.Never);
        }
    }
}
