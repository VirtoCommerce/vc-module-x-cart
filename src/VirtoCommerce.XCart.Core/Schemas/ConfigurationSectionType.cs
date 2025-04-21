using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationSectionType : ExtendableGraphType<ExpProductConfigurationSection>
{
    public ConfigurationSectionType()
    {
        Field(x => x.Id, nullable: false).Description("Configuration section id");
        Field(x => x.Name, nullable: true).Description("Configuration section name");
        Field(x => x.Description, nullable: true).Description("Configuration section description");
        Field(x => x.IsRequired, nullable: false).Description("Is configuration section required");
        Field(x => x.Type, nullable: false).Description("Configuration section type. Possible values: 'Product', 'Text', 'File'");
        Field(x => x.AllowCustomText, nullable: false).Description("Is custom text allowed for Text-type section");
        Field(x => x.AllowTextOptions, nullable: false).Description("Is predefined text options allowed for Text-type section");

        ExtendableField<ListGraphType<ConfigurationOptionType>>(
            nameof(ExpProductConfigurationSection.Options),
            resolve: context => context.Source.Options);
    }
}
