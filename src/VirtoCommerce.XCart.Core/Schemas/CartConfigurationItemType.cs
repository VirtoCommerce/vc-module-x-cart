using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartConfigurationItemType : ExtendableGraphType<ConfigurationItem>
    {
        public CartConfigurationItemType()
        {
            Field(x => x.Id, nullable: false).Description("Configuration item ID");
            Field(x => x.Name, nullable: true).Description("Configuration item name");
        }
    }
}
