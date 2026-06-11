using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartTotalType : ExtendableGraphType<CartTotalAggregate>
    {
        public CartTotalType()
        {
            Field(x => x.IsDefaultTotalCurrency, nullable: false).Description("Is current total in default total currency");

            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Cart total")
                .Resolve(context => context.GetTotal(context.Source.CartTotal.Total));

            Field<NonNullGraphType<MoneyType>>("subTotal")
                .Description("Cart subtotal")
                .Resolve(context => context.GetTotal(context.Source.CartTotal.SubTotal));

            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Total tax")
                .Resolve(context => context.GetTotal(context.Source.CartTotal.TaxTotal));

            Field<NonNullGraphType<MoneyType>>("discountTotal")
                .Description("Total discount")
                .Resolve(context => context.GetTotal(context.Source.CartTotal.DiscountTotal));
        }
    }
}
