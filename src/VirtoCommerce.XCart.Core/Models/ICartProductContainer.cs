using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public interface ICartProductContainer
{
    public Store Store { get; set; }
    public string CultureName { get; set; }
    public Currency Currency { get; set; }
    public Member Member { get; set; }
    public string UserId { get; set; }
    string OrganizationId { get; set; }
    public IList<string> ProductsIncludeFields { get; set; }
}
