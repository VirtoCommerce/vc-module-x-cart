using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class SharingSettingType : ExtendableGraphType<CartSharingSetting>
    {
        public SharingSettingType(ICartSharingService cartSharingService, IDataLoaderContextAccessor dataLoader)
        {
            Field(x => x.Id, nullable: false).Description("Id (sharing key)");
            Field<StringGraphType>("SharedWithId")
                .Description("Id of the first target the list is shared with; owner only, null for non-targeted scopes")
                .DeprecationReason("Use targets")
                .Resolve(context => ResolveIsOwner(context) ? context.Source.Targets?.FirstOrDefault()?.SharedWithId : null);
            Field(x => x.Message, nullable: true).Description("Message saved with the share (one for all targets)");
            AddField(new FieldType
            {
                Name = "Targets",
                Description = "Targets the list is shared with (id space defined by scope); owner only, empty for other viewers and for non-targeted scopes",
                Type = GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<ListGraphType<NonNullGraphType<SharingTargetType>>>>(),
                Resolver = new FuncFieldResolver<CartSharingSetting, IDataLoaderResult<WishlistSharingTarget[]>>(context =>
                    ResolveIsOwner(context)
                        ? dataLoader.LoadSharingTargets(cartSharingService, context.Source)
                        : SharingTargetDataLoaderExtensions.NoTargets),
            });
            Field<WishlistScopeType>("Scope").Description("Scope (private, organization, etc.)").Resolve(context => context.Source.Scope);
            Field<WishlistAccessType>("Access").Description("Access (read or write)").Resolve(context => context.Source.Access);
            Field<bool>("IsOwner", nullable: false).Description("Created by current user").Resolve(ResolveIsOwner);
        }

        // Who a list is shared with is the owner's information; a targeted reader must not learn the other recipients.
        protected virtual bool ResolveIsOwner(IResolveFieldContext<CartSharingSetting> context)
        {
            return context.Source.CreatedBy.EqualsIgnoreCase(context.User.GetUserId());
        }
    }
}
