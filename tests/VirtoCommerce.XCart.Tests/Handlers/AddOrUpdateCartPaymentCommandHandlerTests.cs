using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Tests.Helpers;
using VirtoCommerce.XCart.Tests.Helpers.Stubs;
using Xunit;


namespace VirtoCommerce.XCart.Tests.Handlers
{
    public class AddOrUpdateCartPaymentCommandHandlerTests : XCartMoqHelper
    {
        [Fact]
        public async Task Handle_RequestWithPayments_AllPaymentFieldsAreMapped()
        {
            // Arrange
            var payment = _fixture.Create<ExpCartPayment>();

            var cartAggregate = GetValidCartAggregate();
            cartAggregate.Cart.Payments.Clear();

            var cartAggregateRepositoryMock = new Mock<ICartAggregateRepository>();
            cartAggregateRepositoryMock
                .Setup(x => x.GetCartByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(cartAggregate);

            var availablePaymentMethods = new Mock<ICartAvailMethodsService>();
            availablePaymentMethods
                .Setup(x => x.GetAvailablePaymentMethodsAsync(It.Is<CartAggregate>(y => y == cartAggregate)))
                .ReturnsAsync(new List<PaymentMethod>()
                {
                    new StubPaymentMethod(payment.PaymentGatewayCode.Value)
                });

            var request = new AddOrUpdateCartPaymentCommand()
            {
                Payment = payment,
                CartId = cartAggregate.Cart.Id,
            };
            var handler = new AddOrUpdateCartPaymentCommandHandler(cartAggregateRepositoryMock.Object, availablePaymentMethods.Object);

            // Act
            var aggregate = await handler.Handle(request, CancellationToken.None);

            // Assert
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.Id == payment.Id.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.OuterId == payment.OuterId.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.PaymentGatewayCode == payment.PaymentGatewayCode.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.Currency == payment.Currency.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.Price == payment.Price.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.Amount == payment.Amount.Value);
            cartAggregate.Cart.Payments.Should().ContainSingle(x => x.BillingAddress != null);
        }
    }
}
