using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class CartProductsRequest
{
    public Store Store { get; set; }

    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public Currency Currency { get; set; }
    public string CurrencyCode { get; set; }

    public Member Member { get; set; }
    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public IList<string> ProductIds { get; set; }
    public IList<string> ProductsIncludeFields { get; set; }

    public bool LoadPrice { get; set; } = true;
    public bool LoadInventory { get; set; } = true;
    public bool EvaluatePromotions { get; set; } = false;
}
