using FluentValidation;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// Wrapper for the existing IConfigurationItemValidator as a link in the validation chain
/// </summary>
public class ConfigurationItemContextValidator : AbstractValidator<ConfigurationItemValidationContext>, ICartValidator<ConfigurationItemValidationContext>
{
    public int Order => ModuleConstants.ValidationOrder.Core;

    public ConfigurationItemContextValidator(IConfigurationItemValidator configurationItemValidator)
    {
        RuleFor(x => x.LineItem).CustomAsync(async (lineItem, context, cancellationToken) =>
        {
            if (lineItem == null)
            {
                return;
            }

            var result = await configurationItemValidator.ValidateAsync(lineItem, cancellationToken);
            foreach (var error in result.Errors)
            {
                context.AddFailure(error);
            }
        });
    }
}
