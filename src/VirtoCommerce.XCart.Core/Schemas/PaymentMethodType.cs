using GraphQL.Types;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class PaymentMethodType : ObjectGraphType<PaymentMethod>
    {
        public PaymentMethodType()
        {
            Field(x => x.Code, nullable: false).Description("Value of payment gateway code");
            Field(x => x.Name, nullable: true).Description("Value of payment method name");
            Field(x => x.Description, nullable: true).Description("Payment method description");
            Field(x => x.LogoUrl, nullable: true).Description("Value of payment method logo absolute URL");
            Field(x => x.Priority, nullable: false).Description("Value of payment method priority");
            Field(x => x.IsAvailableForPartial, nullable: false).Description("Is payment method available for partial payments");

            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.GetCart().Currency);

            Field<NonNullGraphType<MoneyType>>("price",
                "Price",
                resolve: context => context.Source.Price.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("priceWithTax",
                "Price with tax",
                resolve: context => context.Source.PriceWithTax.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("discountAmount",
                "Discount amount",
                resolve: context => context.Source.DiscountAmount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmountWithTax",
                "Discount amount with tax",
                resolve: context => context.Source.DiscountAmountWithTax.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("total",
                "Total",
                resolve: context => context.Source.Total.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("totalWithTax",
                "Total with tax",
                resolve: context => context.Source.TotalWithTax.ToMoney(context.GetCart().Currency));

            Field(x => x.TaxType, nullable: true).Description("Tax type");
            Field(x => x.TaxPercentRate, nullable: false).Description("Tax percent rate");
            Field<NonNullGraphType<MoneyType>>("taxTotal",
                "Tax total",
                resolve: context => context.Source.TaxTotal.ToMoney(context.GetCart().Currency));
            Field<ListGraphType<NonNullGraphType<TaxDetailType>>>("taxDetails",
                "Tax details",
                resolve: context => context.Source.TaxDetails);

            Field<NonNullGraphType<StringGraphType>>("paymentMethodType",
                "Value of payment method type",
                resolve: context => context.Source.PaymentMethodType.ToString());
            Field<NonNullGraphType<StringGraphType>>("paymentMethodGroupType",
                "Value of payment group type",
                resolve: context => context.Source.PaymentMethodGroupType.ToString());
        }
    }
}
