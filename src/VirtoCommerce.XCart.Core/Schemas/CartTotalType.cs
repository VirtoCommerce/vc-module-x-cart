using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartTotalType : ExtendableGraphType<CartTotalAggregate>
    {
        public CartTotalType()
        {
            Field(x => x.IsDefaultTotalCurrency, nullable: false).Description("Is current total in default total currency");

            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Cart total")
                .Resolve(context => context.Source.CartTotal.Total.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("subTotal")
                .Description("Cart subtotal")
                .Resolve(context => context.Source.CartTotal.SubTotal.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Total tax")
                .Resolve(context => context.Source.CartTotal.TaxTotal.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("discountTotal")
                .Description("Total discount")
                .Resolve(context => context.Source.CartTotal.DiscountTotal.ToMoney(context.Source.Currency));
        }
    }
}
