using GraphQL;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Core.Extensions
{
    public static class ResolveFieldContextExtensions
    {
        public static CartAggregate GetCart(this IResolveFieldContext userContext)
        {
            return userContext.GetValueForSource<CartAggregate>();
        }

        public static Currency CartCurrency(this IResolveFieldContext userContext)
        {
            return userContext.GetValueForSource<CartAggregate>().Currency;
        }

        public static Money GetTotal(this IResolveFieldContext<CartAggregate> context, decimal number)
        {
            return context.Source.HasSelectedLineItems
                ? number.ToMoney(context.Source.Currency)
                : new Money(0.0m, context.Source.Currency);
        }

        public static T GetCartCommand<T>(this IResolveFieldContext context)
            where T : ICartRequest
        {
            var cartCommand = (T)context.GetArgument(GenericTypeHelper.GetActualType<T>(), SchemaConstants.CommandName);

            if (cartCommand != null)
            {
                cartCommand.OrganizationId = context.GetCurrentOrganizationId();
            }

            return cartCommand;
        }

        public static T GetCartQuery<T>(this IResolveFieldContext context) where T : ICartQuery
        {
            var result = AbstractTypeFactory<T>.TryCreateInstance();
            result.StoreId = context.GetArgumentOrValue<string>("storeId");
            result.UserId = context.GetArgumentOrValue<string>("userId");
            result.OrganizationId = context.GetCurrentOrganizationId();
            result.CurrencyCode = context.GetArgumentOrValue<string>("currencyCode");
            result.CultureName = context.GetArgumentOrValue<string>("cultureName");
            result.CartType = context.GetArgumentOrValue<string>("cartType");
            result.CartName = context.GetArgumentOrValue<string>("cartName");

            return result;
        }
    }
}
