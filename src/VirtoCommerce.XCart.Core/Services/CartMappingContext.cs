namespace VirtoCommerce.XCart.Core.Services;

public class CartMappingContext
{
    public string CultureName { get; set; }
    public string CurrencyCode { get; set; }
    public ICartItemBuilder Builder { get; set; }
}
