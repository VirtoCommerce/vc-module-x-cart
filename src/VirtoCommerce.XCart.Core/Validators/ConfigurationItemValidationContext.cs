using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Validators
{
    /// <summary>
    /// Validation context for a configured line item, wraps the LineItem.
    /// </summary>
    public class ConfigurationItemValidationContext
    {
        public LineItem LineItem { get; set; }
    }

}
