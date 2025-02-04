using VirtoCommerce.CatalogModule.Core.Model.Configuration;

namespace VirtoCommerce.XCart.Core.Models;

public class ProductConfigurationSection
{
    public string SectionId { get; set; }

    public string CustomText { get; set; }

    public ProductConfigurationSectionType Type { get; set; }

    public ConfigurableProductOption Value { get; set; }
}
