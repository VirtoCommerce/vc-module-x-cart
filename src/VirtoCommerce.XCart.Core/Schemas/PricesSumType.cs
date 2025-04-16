using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class PricesSumType : ObjectGraphType<ExpPricesSum>
    {
        public PricesSumType()
        {
            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Total price")
                .Resolve(context => context.Source.Total.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("discountTotal")
                .Description("Total discount amount")
                .Resolve(context => context.Source.DiscountTotal.ToMoney(context.Source.Currency));
        }
    }
}
