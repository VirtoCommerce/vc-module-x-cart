using System;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistType : ExtendableGraphType<CartAggregate>
    {
        private readonly ICartSharingService _cartSharingService;

        public WishlistType(ICartSharingService cartSharingService)
        {
            _cartSharingService = cartSharingService;

            Field(x => x.Cart.Id, nullable: false).Description("Shopping cart ID");
            Field(x => x.Cart.Name, nullable: false).Description("Shopping cart name");
            Field(x => x.Cart.StoreId, nullable: true).Description("Shopping cart store ID");
            Field(x => x.Cart.CustomerId, nullable: true).Description("Shopping cart user ID");
            Field(x => x.Cart.CustomerName, nullable: true).Description("Shopping cart user name");
            Field<CurrencyType>("currency").Description("Currency").Resolve(context => context.Source.Currency);
            ExtendableField<ListGraphType<LineItemType>>("items", "Items", resolve: context => context.Source.LineItems);
            Field<IntGraphType>("itemsCount").Description("Item count").Resolve(context => context.Source.Cart.LineItemsCount);
            ExtendableField<WishlistScopeType>("Scope", "Wishlist scope", resolve: context => (ResolveSharingSetting(context) as CartSharingSetting)?.Scope, deprecationReason: "Use SharingSetting.Scope instead");
            Field(x => x.Cart.Description, nullable: true).Description("Wishlist description");
            Field(x => x.Cart.ModifiedDate, nullable: true).Description("Wishlist modified date");
            Field<NonNullGraphType<MoneyType>>("subTotal").Description("Wishlist subtotal").Resolve(context => context.GetTotal(context.Source.Cart.SubTotal));
            ExtendableField<SharingSettingType>("SharingSetting", "Sharing settings", resolve: ResolveSharingSetting);
        }

        protected virtual object ResolveSharingSetting(IResolveFieldContext<CartAggregate> context)
        {
            var result = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            result.Id = context.Source.Cart.SharingSettings.FirstOrDefault()?.Id ?? Guid.NewGuid().ToString();

            result.CreatedBy = _cartSharingService.GetSharingOwnerUserId(context.Source.Cart);//TODO: refactor
            result.Scope = _cartSharingService.GetSharingScope(context.Source.Cart);
            result.Access = _cartSharingService.GetSharingAccess(context.Source.Cart, context.User.GetUserId());

            return result;
        }
    }
}
