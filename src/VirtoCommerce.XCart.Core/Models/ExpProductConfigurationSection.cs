using System.Collections.Generic;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;

namespace VirtoCommerce.XCart.Core.Models;

public class ExpProductConfigurationSection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public string Type { get; set; }

    public IList<ExpConfigurationLineItem> Options { get; set; } = [];
}

public class ProductConfigurationQueryResponse
{
    public IList<ExpProductConfigurationSection> ConfigurationSections { get; set; } = [];
}
