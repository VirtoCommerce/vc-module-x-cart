using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public interface ICartProductContainer
{
    public Store Store { get; }
    public string CultureName { get; }
    public Currency Currency { get; }
    public Member Member { get; }
    public string UserId { get; }
    public IList<string> ProductsIncludeFields { get; }
}
