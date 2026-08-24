using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public class CartMappingContext : MappingContext
{
    public ICartItemBuilder Builder { get; set; }
}
