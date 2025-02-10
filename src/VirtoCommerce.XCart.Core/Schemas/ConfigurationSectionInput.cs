using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationSectionInput : InputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId")
            .Description("Configuration section ID");
        Field<StringGraphType>("customText")
            .Description("Custom text for 'Text' type section");
        Field<NonNullGraphType<CartConfigurationItemEnumType>>("type")
            .Description("Configuration section type");
        Field<ConfigurableProductOptionInput>("value")
            .Description("Deprecated! Use Option property instead")
            .DeprecationReason("Use Option property instead");
        Field<ConfigurableProductOptionInput>("option")
            .Description("Configuration section option/product");
    }
}
