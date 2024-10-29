using System.Linq;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ConfiguredLineItemType : ExtendableGraphType<ConfiguredLineItemAggregate>
    {
        // prepare only total fields
        public ConfiguredLineItemType()
        {
            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.Source.Currency);

            Field<NonNullGraphType<MoneyType>>("total",
                "Shopping cart total",
                resolve: context => context.Source.Cart.Total.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("taxTotal",
                "Total tax",
            resolve: context => context.Source.Cart.TaxTotal.ToMoney(context.Source.Currency));

            // subtotal
            Field<NonNullGraphType<MoneyType>>("subTotal",
                "Shopping cart subtotal",
                resolve: context => context.Source.Cart.SubTotal.ToMoney(context.Source.Currency));
            Field<NonNullGraphType<MoneyType>>("subTotalWithTax",
                "Subtotal with tax",
                resolve: context => context.Source.Cart.SubTotalWithTax.ToMoney(context.Source.Currency));

            // extended price
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotal",
                "Total extended price",
                resolve: context => context.Source.Cart.Items.Sum(i => i.ExtendedPrice).ToMoney(context.Source.Currency));
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotalWithTax",
                "Total extended price with tax",
                resolve: context => context.Source.Cart.Items.Sum(i => i.ExtendedPriceWithTax).ToMoney(context.Source.Currency));

            // discount
            Field<NonNullGraphType<MoneyType>>("discountTotal",
                "Total discount",
                resolve: context => context.Source.Cart.DiscountTotal.ToMoney(context.Source.Currency));
            Field<NonNullGraphType<MoneyType>>("discountTotalWithTax",
                "Total discount with tax",
                resolve: context => context.Source.Cart.DiscountTotalWithTax.ToMoney(context.Source.Currency));
        }
    }
}
