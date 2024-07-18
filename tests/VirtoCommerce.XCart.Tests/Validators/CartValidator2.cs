using FluentValidation;
using FluentValidation.Results;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Tests.Validators
{
    /// <summary>
    /// Check ApplyRuleXXXXX methods are working in the overridden validator
    /// </summary>
    public class CartValidator2 : CartValidator
    {
        protected override void ApplyRuleForItems(CartValidationContext cartContext, ValidationContext<CartValidationContext> context)
        {
            var result = new ValidationResult(new ValidationFailure[] { new ValidationFailure("FakeFailure", "FakeFailure") });
            result.Errors.Apply(x => context.AddFailure(x));
        }
    }
}
