using GraphQL;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class SharingSettingType : ExtendableGraphType<CartSharingSetting>
    {
        public SharingSettingType()
        {
            Field(x => x.Id, nullable: false).Description("Id (sharing key)");
            Field<WishlistScopeType>("Scope").Description("Scope (private, organization, etc.)").Resolve(context => context.Source.Scope);
            Field<WishlistAccessType>("Access").Description("Access (read or write)").Resolve(context => context.Source.Access);
            Field<bool>("IsOwner", nullable: false).Description("Created by current user").Resolve(ResolveIsOwner);
        }

        protected virtual bool ResolveIsOwner(IResolveFieldContext<CartSharingSetting> context)
        {
            return context.Source.CreatedBy == context.User.GetUserId();
        }
    }
}
