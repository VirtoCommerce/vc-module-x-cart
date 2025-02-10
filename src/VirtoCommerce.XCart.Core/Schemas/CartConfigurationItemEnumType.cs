using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Schemas;

public class CartConfigurationItemEnumType : EnumerationGraphType<ConfigurationItemType>
{
    public CartConfigurationItemEnumType()
    {
        Name = "CartConfigurationItemEnumType";
    }
}
