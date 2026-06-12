using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Validators
{
    /// <summary>
    /// Validation context for a configured line item. Wraps the <see cref="LineItem"/> so the
    /// configuration validation chain is keyed by a dedicated type rather than the shared
    /// <see cref="LineItem"/> — this keeps it isolated from any other LineItem-based validators and
    /// lets other modules contribute <see cref="ICartValidator{ConfigurationItemValidationContext}"/>
    /// without colliding with unrelated validation.
    /// </summary>
    public class ConfigurationItemValidationContext
    {
        public LineItem LineItem { get; set; }
    }

}
