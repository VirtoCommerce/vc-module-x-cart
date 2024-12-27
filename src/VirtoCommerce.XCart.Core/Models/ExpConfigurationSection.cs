using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class ExpProductConfigurationSection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }

    public IList<ExpConfigurationLineItem> Options { get; set; } = new List<ExpConfigurationLineItem>();
}

public class ProductConfigurationQueryResponse
{
    public IList<ExpProductConfigurationSection> ConfigurationSections { get; set; } = new List<ExpProductConfigurationSection>();
}
