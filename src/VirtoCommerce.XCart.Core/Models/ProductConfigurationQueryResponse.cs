using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class ProductConfigurationQueryResponse
{
    public IList<ExpProductConfigurationSection> ConfigurationSections { get; set; } = [];
}
