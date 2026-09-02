using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public class CartMappingContext : MappingContext
{
    public NewCartItem NewCartItem { get; set; }
}
