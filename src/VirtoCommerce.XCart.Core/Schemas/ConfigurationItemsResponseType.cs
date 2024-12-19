using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurationItemsResponseType : ExtendableGraphType<ConfigurationItemsResponse>
{
    public ConfigurationItemsResponseType()
    {
        ExtendableField<ListGraphType<CartConfigurationItemType>>(
            "configurationItems",
            "Configuration items for configurable product",
            resolve: context => context.Source.ConfigurationItems ?? []);
    }
}
