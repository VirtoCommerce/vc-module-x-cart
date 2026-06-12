using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Data.Validators;

/// <summary>
/// Default <see cref="ICartValidatorRegistry"/>. Resolves the validator chain from the ambient
/// <see cref="IServiceProvider"/> (the cart aggregate's scope), so scoped validators such as the
/// configuration-item validator and module-supplied validators bind to the right scope.
/// </summary>
public class CartValidatorRegistry(IServiceProvider serviceProvider) : ICartValidatorRegistry
{
    public async Task<IList<ValidationFailure>> ValidateAsync<TContext>(TContext context, Action<ValidationStrategy<TContext>> options = null)
    {
        var errors = new List<ValidationFailure>();

        var validators = serviceProvider
            .GetServices<ICartValidator<TContext>>()
            .OrderBy(x => x.Order);

        foreach (var validator in validators)
        {
            // Throws here if the caller opted into ThrowOnFailures — chain stops at the first failing link.
            var result = options is null
                ? await validator.ValidateAsync(context)
                : await validator.ValidateAsync(context, options);

            errors.AddRange(result.Errors);
        }

        return errors;
    }
}
