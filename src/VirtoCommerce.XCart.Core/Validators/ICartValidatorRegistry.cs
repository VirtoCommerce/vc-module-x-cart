using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Internal;
using FluentValidation.Results;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// Resolves every ICartValidator{TContext} registered for TContext, runs them in order and returns the combined failures.
/// </summary>
public interface ICartValidatorRegistry
{
    Task<IList<ValidationFailure>> ValidateAsync<TContext>(TContext context, Action<ValidationStrategy<TContext>> options = null);
}
