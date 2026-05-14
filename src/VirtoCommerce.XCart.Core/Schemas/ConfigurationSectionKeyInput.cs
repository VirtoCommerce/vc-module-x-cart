using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

/// <summary>
/// Identifying subset of <see cref="ProductConfigurationSection"/> sufficient to locate
/// an existing <see cref="VirtoCommerce.CartModule.Core.Model.ConfigurationItem"/> on a line item.
/// Used by mutations that act on an already-configured item (e.g. selection toggling)
/// where the full configuration payload is not relevant.
/// </summary>
public class ConfigurationSectionKeyInput : ExtendableInputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionKeyInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId").Description("Configuration section ID");
        Field<NonNullGraphType<StringGraphType>>("type").Description("Configuration section type. Possible values: 'Product', 'Variation', 'Text', 'File'");
        Field<ConfigurableProductOptionKeyInput>("option").Description("Identifying subset of the configuration section option (Product/Variation only)");
    }
}
