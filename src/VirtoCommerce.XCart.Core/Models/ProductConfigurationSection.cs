using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class ProductConfigurationSection
{
    public string SectionId { get; set; }

    public string Type { get; set; }

    public ConfigurableProductOption Option { get; set; }

    public string CustomText { get; set; }

    public IList<string> FileUrls { get; set; } = [];
}
