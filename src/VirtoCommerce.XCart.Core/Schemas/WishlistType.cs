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
            // Directly, not off ResolveSharingSetting: that builds a whole setting - a new key, three service calls
            // and a sort of every target - for one string this field then reads back.
            ExtendableField<WishlistScopeType>("Scope", "Wishlist scope", resolve: context => _cartSharingService.GetSharingScope(context.Source.Cart), deprecationReason: "Use SharingSetting.Scope instead");
            Field(x => x.Cart.Description, nullable: true).Description("Wishlist description");
            Field(x => x.Cart.ModifiedDate, nullable: true).Description("Wishlist modified date");
            Field<NonNullGraphType<MoneyType>>("subTotal").Description("Wishlist subtotal").Resolve(context => context.GetTotal(context.Source.Cart.SubTotal));
            ExtendableField<SharingSettingType>("SharingSetting", "Sharing settings", resolve: ResolveSharingSetting);
        }

        protected virtual object ResolveSharingSetting(IResolveFieldContext<CartAggregate> context)
        {
            var result = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            var existingSetting = context.Source.Cart.GetEffectiveSharingSetting();

            result.Id = existingSetting?.Id ?? Guid.NewGuid().ToString();

            result.CreatedBy = _cartSharingService.GetSharingOwnerUserId(context.Source.Cart);//TODO: refactor
            result.Scope = _cartSharingService.GetSharingScope(context.Source.Cart);
            result.Access = _cartSharingService.GetSharingAccess(context.Source.Cart, context.User.GetUserId());
            result.Message = existingSetting?.Message;
            // One order on every surface. A mutation response carries the in-memory order and a read whatever
            // the database returns, so "the first target" - which is what the deprecated sharedWithId means -
            // differed between them (VCST-6152). Ordered by id: the only key populated in both.
            result.Targets = existingSetting?.Targets?.OrderBy(x => x.SharedWithId, StringComparer.OrdinalIgnoreCase).ToList();

            return result;
        }
    }
}
