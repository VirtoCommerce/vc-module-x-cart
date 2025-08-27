using System;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistType : ExtendableGraphType<CartAggregate>
    {
        public WishlistType()
        {
            Field(x => x.Cart.Id, nullable: false).Description("Shopping cart ID");
            Field(x => x.Cart.Name, nullable: false).Description("Shopping cart name");
            Field(x => x.Cart.StoreId, nullable: true).Description("Shopping cart store ID");
            Field(x => x.Cart.CustomerId, nullable: true).Description("Shopping cart user ID");
            Field(x => x.Cart.CustomerName, nullable: true).Description("Shopping cart user name");
            Field<CurrencyType>("currency").Description("Currency").Resolve(context => context.Source.Currency);
            ExtendableField<ListGraphType<LineItemType>>("items", "Items", resolve: context => context.Source.LineItems);
            Field<IntGraphType>("itemsCount").Description("Item count").Resolve(context => context.Source.Cart.LineItemsCount);
            ExtendableField<WishlistScopeType>(nameof(CartAggregate.Scope), "Wishlist scope", resolve: context => context.Source.Scope);
            Field(x => x.Cart.Description, nullable: true).Description("Wishlist description");
            Field(x => x.Cart.ModifiedDate, nullable: true).Description("Wishlist modified date");
            ExtendableField<SharingSettingType>("SharingSetting", "Sharing settings", resolve: ResolveSharingSetting);
        }

        protected virtual object ResolveSharingSetting(IResolveFieldContext<CartAggregate> context)
        {
            var sharingSetting = context.Source.Cart.SharingSettings.FirstOrDefault();

            if (sharingSetting != null)
            {
                return sharingSetting;
            }

            return new CartSharingSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Access = CartSharingAccess.Read,
                Scope = context.Source.Scope ?? CartSharingScope.Private,
            };
        }
    }
}
