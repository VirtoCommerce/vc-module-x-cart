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
        Field(x => x.Type, nullable: true).Description("Configuration section type");
        Field(x => x.Description, nullable: true).Description("Configuration section description");
        Field(x => x.IsRequired, nullable: false).Description("Is configuration section required");

        ExtendableField<ListGraphType<ConfigurationLineItemType>>(
            nameof(ExpProductConfigurationSection.Options),
            resolve: context => context.Source.Options);
    }
}
