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
            // Throws here if the caller called ThrowOnFailures(), chain stops at the first failing link.
            var result = options is null
                ? await validator.ValidateAsync(context)
                : await validator.ValidateAsync(context, options);

            errors.AddRange(result.Errors);
        }

        return errors;
    }
}
