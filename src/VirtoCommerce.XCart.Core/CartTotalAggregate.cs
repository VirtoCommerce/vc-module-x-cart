using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core;

public class CartTotalAggregate
{
    public bool IsDefaultTotalCurrency { get; set; }

    public CartAggregate CartAggregate { get; set; }

    public Currency Currency { get; set; }

    public CartTotal CartTotal { get; set; }
}
