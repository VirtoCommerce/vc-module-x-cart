using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class PricesSumType : ObjectGraphType<ExpPricesSum>
    {
        public PricesSumType()
        {
            Field<NonNullGraphType<MoneyType>>("listPrice")
                .Description("List price")
                .Resolve(context => context.Source.ListPriceSum.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("salePrice")
                .Description("Sale price")
                .Resolve(context => context.Source.SalePriceSum.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("discountAmount")
                .Description("Total discount amount")
                .Resolve(context => context.Source.DiscountAmountSum.ToMoney(context.Source.Currency));
        }
    }
}
