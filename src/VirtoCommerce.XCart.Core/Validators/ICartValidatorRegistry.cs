using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Internal;
using FluentValidation.Results;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// Resolves every <see cref="ICartValidator{TContext}"/> registered for <typeparamref name="TContext"/>,
/// runs them in <see cref="ICartValidator{TContext}.Order"/> order and returns the combined failures.
/// This is the single entry point the cart aggregate uses to validate; built-in validators are the
/// first links of the chain and other modules append their own.
/// </summary>
public interface ICartValidatorRegistry
{
    /// <param name="context">The object to validate.</param>
    /// <param name="options">
    /// Optional FluentValidation strategy applied to every link (e.g. IncludeRuleSets(...).ThrowOnFailures() etc) to make a failing link throw
    /// the chain stops at the first throwing validator; the core validator runs first.
    /// </param>
    Task<IList<ValidationFailure>> ValidateAsync<TContext>(TContext context, Action<ValidationStrategy<TContext>> options = null);
}
