using FluentValidation;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// Core link of the configuration validation chain. Adapts the existing
/// <see cref="IConfigurationItemValidator"/> (which validates a bare LineItem) to the
/// <see cref="ConfigurationItemValidationContext"/> the chain is keyed by, so the built-in
/// configuration rules keep running while other modules can append their own links.
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
