using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    /// <summary>
    /// Constructs cart items — <see cref="LineItem"/> and <see cref="ConfigurationItem"/> —
    /// during cart mutation.
    /// </summary>
    public interface ICartItemBuilder
    {
        LineItem Create(CartProduct cartProduct);

        ConfigurationItem Create(ProductConfigurationSection configurationSection, CartProduct cartProduct = null);
    }
}
