using FluentValidation;

namespace VirtoCommerce.XCart.Core.Validators;

/// <summary>
/// A single link in the cart validation chain for a given TContext.
/// Built-in validators and validators contributed by other modules implement this interface.
/// </summary>
public interface ICartValidator<in TContext> : IValidator<TContext>
{
    /// <summary>
    /// Relative position in the chain. Lower runs first.
    /// </summary>
    int Order => 0;
}
