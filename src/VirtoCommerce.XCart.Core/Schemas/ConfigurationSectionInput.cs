using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationSectionInput : InputObjectGraphType<ProductConfigurationSection>
{
    public ConfigurationSectionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("sectionId").Description("Configuration section ID");
        Field<NonNullGraphType<StringGraphType>>("type").Description("Configuration section type. Possible values: 'Product', 'Text', 'File'");
        Field<ConfigurableProductOptionInput>("option").Description("Configuration section option/product");
        Field<StringGraphType>("customText").Description("Custom text for 'Text' type section");
    }
}
