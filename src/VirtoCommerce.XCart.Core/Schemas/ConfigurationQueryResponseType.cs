using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationQueryResponseType : ExtendableGraphType<ProductConfigurationQueryResponse>
{
    public ConfigurationQueryResponseType()
    {
        Field<ListGraphType<ConfigurationSectionType>>(nameof(ProductConfigurationQueryResponse.ConfigurationSections))
            .Resolve(context => context.Source.ConfigurationSections);
    }
}
