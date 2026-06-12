using FluentValidation;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// A single link in the cart validation chain for a given <typeparamref name="TContext"/>.
/// It is a plain FluentValidation <see cref="IValidator{T}"/> plus an <see cref="Order"/>; the
/// <see cref="ICartValidatorRegistry"/> resolves all registered links for a context type, runs
/// them ordered by <see cref="Order"/> via the native <c>ValidateAsync</c> and aggregates failures.
/// Built-in validators and validators contributed by other modules implement this interface.
/// </summary>
/// <typeparam name="TContext">The object being validated (e.g. CartValidationContext, NewCartItem).</typeparam>
public interface ICartValidator<TContext> : IValidator<TContext>
{
    /// <summary>
    /// Relative position in the chain. Lower runs first; built-in validators use
    /// <see cref="ModuleConstants.ValidationOrder.Core"/> so they run before extensions.
    /// </summary>
    int Order => 0;
}
