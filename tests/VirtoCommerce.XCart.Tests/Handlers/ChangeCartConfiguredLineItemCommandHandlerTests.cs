using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class ChangeCartConfiguredLineItemCommandHandlerTests
    {
        private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();

        private ChangeCartConfiguredLineItemCommandHandler CreateHandler()
        {
            return new ChangeCartConfiguredLineItemCommandHandler(
                _cartAggregateRepositoryMock.Object,
                _mediatorMock.Object);
        }

        private static Mock<CartAggregate> CreateCartAggregateMock()
        {
            var mock = new Mock<CartAggregate>(
                MockBehavior.Loose, null, null, null, null, null, null, null, null, null, null, null, null, null);

            // Cart property has a protected setter and is non-virtual, so we set it via reflection
            var cartProperty = typeof(CartAggregate).GetProperty(nameof(CartAggregate.Cart));
            cartProperty.SetValue(mock.Object, new ShoppingCart { Id = "cart-1" });

            return mock;
        }

        [Fact]
        public async Task Handle_ProductSection_PreservesSelectedForCheckoutFromOldItem()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            // Old: Product item is unselected. Product changed within the same section.
            var oldLineItem = new LineItem
            {
                Id = "line-1",
                ProductId = "configurable-1",
                IsConfigured = true,
                ConfigurationItems =
                [
                    new() { Type = "Product", SectionId = "section-A", ProductId = "old-product", SelectedForCheckout = false },
                ],
            };
            cartAggregateMock.Setup(x => x.GetConfiguredLineItem("line-1")).Returns(oldLineItem);

            var mediatorResult = new ExpConfigurationLineItem
            {
                Item = new LineItem
                {
                    ConfigurationItems =
                    [
                        new() { Type = "Product", SectionId = "section-A", ProductId = "new-product", SelectedForCheckout = true },
                    ],
                },
            };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mediatorResult);

            var handler = CreateHandler();
            var request = new ChangeCartConfiguredLineItemCommand
            {
                CartId = "cart-1",
                LineItemId = "line-1",
                ConfigurationSections = [],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert - preserved from old (false), even though productId changed within the section.
            cartAggregateMock.Verify(x => x.UpdateConfiguredLineItemAsync(
                "line-1",
                It.Is<LineItem>(li =>
                    li.ConfigurationItems.Single().ProductId == "new-product" &&
                    li.ConfigurationItems.Single().SelectedForCheckout == false)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_TextSection_PreservesSelectedForCheckoutFromOldItem()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            // Old Text section is unselected. Text sections have no Option in the input - preservation still applies.
            var oldLineItem = new LineItem
            {
                Id = "line-1",
                ProductId = "configurable-1",
                IsConfigured = true,
                ConfigurationItems =
                [
                    new() { Type = "Text", SectionId = "section-T", CustomText = "old", SelectedForCheckout = false },
                ],
            };
            cartAggregateMock.Setup(x => x.GetConfiguredLineItem("line-1")).Returns(oldLineItem);

            var mediatorResult = new ExpConfigurationLineItem
            {
                Item = new LineItem
                {
                    ConfigurationItems =
                    [
                        new() { Type = "Text", SectionId = "section-T", CustomText = "new", SelectedForCheckout = true },
                    ],
                },
            };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mediatorResult);

            var handler = CreateHandler();
            var request = new ChangeCartConfiguredLineItemCommand
            {
                CartId = "cart-1",
                LineItemId = "line-1",
                ConfigurationSections = [],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.UpdateConfiguredLineItemAsync(
                "line-1",
                It.Is<LineItem>(li =>
                    li.ConfigurationItems.Single().CustomText == "new" &&
                    li.ConfigurationItems.Single().SelectedForCheckout == false)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_VariationSection_PreservesSelectedForCheckoutPerProductId()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            // Two Variation items share the SectionId - productId disambiguates them.
            var oldLineItem = new LineItem
            {
                Id = "line-1",
                ProductId = "configurable-1",
                IsConfigured = true,
                ConfigurationItems =
                [
                    new() { Type = "Variation", SectionId = "section-V", ProductId = "v1", SelectedForCheckout = false },
                    new() { Type = "Variation", SectionId = "section-V", ProductId = "v2", SelectedForCheckout = true },
                ],
            };
            cartAggregateMock.Setup(x => x.GetConfiguredLineItem("line-1")).Returns(oldLineItem);

            var mediatorResult = new ExpConfigurationLineItem
            {
                Item = new LineItem
                {
                    ConfigurationItems =
                    [
                        new() { Type = "Variation", SectionId = "section-V", ProductId = "v1", SelectedForCheckout = true },
                        new() { Type = "Variation", SectionId = "section-V", ProductId = "v2", SelectedForCheckout = false },
                    ],
                },
            };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mediatorResult);

            var handler = CreateHandler();
            var request = new ChangeCartConfiguredLineItemCommand
            {
                CartId = "cart-1",
                LineItemId = "line-1",
                ConfigurationSections = [],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.UpdateConfiguredLineItemAsync(
                "line-1",
                It.Is<LineItem>(li =>
                    li.ConfigurationItems.First(c => c.ProductId == "v1").SelectedForCheckout == false &&
                    li.ConfigurationItems.First(c => c.ProductId == "v2").SelectedForCheckout == true)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NewSectionWithoutOldMatch_KeepsMediatorValue()
        {
            // Arrange
            var cartAggregateMock = CreateCartAggregateMock();
            _cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregateMock.Object);

            // Old has only section-A; new request introduces a section-B that didn't exist before.
            var oldLineItem = new LineItem
            {
                Id = "line-1",
                ProductId = "configurable-1",
                IsConfigured = true,
                ConfigurationItems =
                [
                    new() { Type = "Product", SectionId = "section-A", ProductId = "p1", SelectedForCheckout = false },
                ],
            };
            cartAggregateMock.Setup(x => x.GetConfiguredLineItem("line-1")).Returns(oldLineItem);

            var mediatorResult = new ExpConfigurationLineItem
            {
                Item = new LineItem
                {
                    ConfigurationItems =
                    [
                        new() { Type = "Product", SectionId = "section-B", ProductId = "p2", SelectedForCheckout = true },
                    ],
                },
            };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<CreateConfiguredLineItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mediatorResult);

            var handler = CreateHandler();
            var request = new ChangeCartConfiguredLineItemCommand
            {
                CartId = "cart-1",
                LineItemId = "line-1",
                ConfigurationSections = [],
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregateMock.Verify(x => x.UpdateConfiguredLineItemAsync(
                "line-1",
                It.Is<LineItem>(li => li.ConfigurationItems.Single().SelectedForCheckout == true)),
                Times.Once);
        }
    }
}
