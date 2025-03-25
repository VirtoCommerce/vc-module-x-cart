using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class CreateConfiguredLineItemCommand : ICommand<ExpConfigurationLineItem>, ICartProductContainerRequest
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public string CurrencyCode { get; set; }

    public string CultureName { get; set; }

    public string ConfigurableProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public bool EvaluatePromotions { get; set; } = false;

    public IList<ProductConfigurationSection> ConfigurationSections { get; set; } = [];

    public string CartId { get; set; }
}
