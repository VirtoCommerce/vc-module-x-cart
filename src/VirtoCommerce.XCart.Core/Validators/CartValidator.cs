using System.Linq;
using FluentValidation;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Core.Validators;

public class CartValidator : AbstractValidator<CartValidationContext>
{
    protected virtual CartLineItemValidator LineItemValidator { get; set; }
    protected virtual CartShipmentValidator ShipmentValidator { get; set; }
    protected virtual CartPaymentValidator PaymentValidator { get; set; }

    public CartValidator()
    {
        LineItemValidator = AbstractTypeFactory<CartLineItemValidator>.TryCreateInstance();
        ShipmentValidator = AbstractTypeFactory<CartShipmentValidator>.TryCreateInstance();
        PaymentValidator = AbstractTypeFactory<CartPaymentValidator>.TryCreateInstance();

        RuleFor(x => x.CartAggregate.Cart).NotNull();
        RuleFor(x => x.CartAggregate.Cart.Name).NotEmpty();
        RuleFor(x => x.CartAggregate.Cart.Currency).NotEmpty();
        RuleFor(x => x.CartAggregate.Cart.CustomerId).NotEmpty();

        RuleSet("items", () =>
            RuleFor(x => x).Custom((cartContext, context) =>
            {
                ApplyRuleForItems(cartContext, context);
            }));

        RuleSet("shipments", () =>
            RuleFor(x => x).Custom((cartContext, context) =>
            {
                ApplyRuleForShipments(cartContext, context);
            }));

        RuleSet("payments", () =>
            RuleFor(x => x).Custom((cartContext, context) =>
            {
                ApplyRuleForPayments(cartContext, context);
            }));

        RuleSet("orderCreate", () =>
            RuleFor(x => x).Custom((cartContext, context) =>
            {
                ApplyRuleForOrderCreate(cartContext, context);
            }));
    }

    protected virtual void ApplyRuleForPayments(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
    {
        cartContext.CartAggregate.Cart.Payments?.Apply(payment =>
        {
            var paymentContext = new PaymentValidationContext
            {
                Payment = payment,
                AvailPaymentMethods = cartContext.AvailPaymentMethods,
            };
            var result = PaymentValidator.Validate(paymentContext);
            result.Errors.Apply(x => context.AddFailure(x));
        });
    }

    protected virtual void ApplyRuleForShipments(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
    {
        cartContext.CartAggregate.Cart.Shipments?.Apply(shipment =>
        {
            var shipmentContext = new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = cartContext.AvailShippingRates,
            };
            var result = ShipmentValidator.Validate(shipmentContext);
            result.Errors.Apply(x => context.AddFailure(x));
        });
    }

    protected virtual void ApplyRuleForItems(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
    {
        cartContext.CartAggregate.SelectedLineItems.Apply(item =>
        {
            var lineItemContext = new LineItemValidationContext
            {
                LineItem = item,
                AllCartProducts = cartContext.AllCartProducts ?? cartContext.CartAggregate.CartProducts.Values,
            };
            var result = LineItemValidator.Validate(lineItemContext);
            result.Errors.Apply(x => context.AddFailure(x));
        });
    }

    protected virtual void ApplyRuleForOrderCreate(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
    {
        // don't use RuleFor here
        if (!cartContext.CartAggregate.SelectedLineItems.Any())
        {
            context.AddFailure(CartErrorDescriber.AllLineItemsUnselected(cartContext.CartAggregate.Cart));
        }

        ValidateConfiguredLineItems(cartContext, context);
    }

    protected virtual void ValidateConfiguredLineItems(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
    {
        if (cartContext.ProductConfigurations.IsNullOrEmpty())
        {
            return;
        }

        foreach (var lineItem in cartContext.CartAggregate.SelectedLineItems.Where(x => x.IsConfigured))
        {
            if (!cartContext.ProductConfigurations.TryGetValue(lineItem.ProductId, out var configuration)
                || configuration.Sections.IsNullOrEmpty())
            {
                continue;
            }

            var selectedSectionIds = lineItem.ConfigurationItems?
                .Where(x => x.SelectedForCheckout)
                .Select(x => x.SectionId)
                .ToHashSet() ?? [];

            var missingRequiredSectionIds = configuration.Sections
                .Where(x => x.IsRequired)
                .Where(x => string.IsNullOrEmpty(x.DependsOnSectionId) || selectedSectionIds.Contains(x.DependsOnSectionId))
                .Select(x => x.Id)
                .Where(x => !selectedSectionIds.Contains(x))
                .ToList();

            if (missingRequiredSectionIds.Count > 0)
            {
                context.AddFailure(CartErrorDescriber.MissingRequiredSections(lineItem, missingRequiredSectionIds));
            }
        }
    }
}
