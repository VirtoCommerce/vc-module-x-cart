using FluentValidation;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class ChangeCartItemPriceValidator : AbstractValidator<PriceAdjustment>, ICartValidator<PriceAdjustment>
    {
        public int Order => ModuleConstants.ValidationOrder.Core;

        public ChangeCartItemPriceValidator()
        {
            RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.LineItemId).NotNull().NotEmpty();
            RuleSet(ModuleConstants.ValidationRuleSets.Strict, () =>
            {
                RuleFor(x => x).Custom((newPriceRequest, context) =>
                {
                    if (newPriceRequest.LineItem != null)
                    {
                        if (newPriceRequest.LineItem.IsGift)
                        {
                            context.AddFailure(CartErrorDescriber.LineItemIsReadOnly(newPriceRequest.LineItem));
                        }

                        var newSalePrice = newPriceRequest.NewPrice;
                        if (newPriceRequest.LineItem.SalePrice > newSalePrice)
                        {
                            context.AddFailure(CartErrorDescriber.UnableToSetLessPrice(newPriceRequest.LineItem));
                        }
                    }
                });
            });
        }
    }
}
