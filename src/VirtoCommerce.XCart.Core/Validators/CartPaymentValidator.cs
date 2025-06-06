using System.Linq;
using FluentValidation;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class CartPaymentValidator : AbstractValidator<PaymentValidationContext>
    {
        public CartPaymentValidator()
        {
            //To support the use case for partial payment update when user sets the address first.
            RuleFor(x => x).Custom((paymentContext, context) =>
            {
                var availPaymentMethods = paymentContext.AvailPaymentMethods;
                var payment = paymentContext.Payment;

                if (availPaymentMethods != null && !string.IsNullOrEmpty(payment.PaymentGatewayCode))
                {
                    var paymentMethod = availPaymentMethods.FirstOrDefault(x => payment.PaymentGatewayCode.EqualsIgnoreCase(x.Code));
                    if (paymentMethod == null)
                    {
                        context.AddFailure(CartErrorDescriber.PaymentMethodUnavailable(payment, payment.PaymentGatewayCode));
                    }
                }
            });
        }
    }
}
