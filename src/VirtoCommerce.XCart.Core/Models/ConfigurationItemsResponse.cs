using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class ConfigurationItemsResponse
{
    public CartAggregate CartAggregate { get; set; }

    public IList<ConfigurationItem> ConfigurationItems { get; set; } = [];
}
