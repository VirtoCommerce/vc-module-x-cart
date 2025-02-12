using System;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class ProductConfigurationSection
{
    public string SectionId { get; set; }

    public string CustomText { get; set; }

    public string Type { get; set; }

    public ConfigurableProductOption Option { get; set; }
}
