using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public class CartMappingContext : MappingContext
{
    public string CurrencyCode { get; set; }
    public ICartItemBuilder Builder { get; set; }
}
