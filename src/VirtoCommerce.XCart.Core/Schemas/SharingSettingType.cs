using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class SharingSettingType : ExtendableGraphType<CartSharingSetting>
    {
        public SharingSettingType()
        {
            Field(x => x.Id, nullable: false).Description("Id (sharing key)");
            Field(x => x.Scope, nullable: false).Description("Scope");
        }
    }
}
