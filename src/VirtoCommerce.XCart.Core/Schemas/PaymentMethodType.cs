using GraphQL;
using GraphQL.Types;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class PaymentMethodType : ExtendableGraphType<PaymentMethod>
    {
        public PaymentMethodType()
        {
            Field(x => x.Code, nullable: false).Description("Value of payment gateway code");
            Field(x => x.Description, nullable: true).Description("Payment method description");
            Field(x => x.LogoUrl, nullable: true).Description("Value of payment method logo absolute URL");
            Field(x => x.Priority, nullable: false).Description("Value of payment method priority");
            Field(x => x.IsAvailableForPartial, nullable: false).Description("Is payment method available for partial payments");

            Field<StringGraphType>("name")
                .Resolve(context => GetLocalizedValue(context, context.Source.LocalizedName, context.Source.Name))
                .Description("Localized name of payment method.");

            Field<NonNullGraphType<CurrencyType>>("currency")
                .Description("Currency")
                .Resolve(context => context.GetCart().Currency);

            Field<NonNullGraphType<MoneyType>>("price")
                .Description("Price")
                .Resolve(context => context.Source.Price.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("priceWithTax")
                .Description("Price with tax")
                .Resolve(context => context.Source.PriceWithTax.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("discountAmount")
                .Description("Discount amount")
                .Resolve(context => context.Source.DiscountAmount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmountWithTax")
                .Description("Discount amount with tax")
                .Resolve(context => context.Source.DiscountAmountWithTax.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Total")
                .Resolve(context => context.Source.Total.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("totalWithTax")
                .Description("Total with tax")
                .Resolve(context => context.Source.TotalWithTax.ToMoney(context.GetCart().Currency));

            Field(x => x.TaxType, nullable: true).Description("Tax type");
            Field(x => x.TaxPercentRate, nullable: false).Description("Tax percent rate");
            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Tax total")
                .Resolve(context => context.Source.TaxTotal.ToMoney(context.GetCart().Currency));
            Field<ListGraphType<NonNullGraphType<TaxDetailType>>>("taxDetails")
                .Description("Tax details")
                .Resolve(context => context.Source.TaxDetails);

            Field<NonNullGraphType<StringGraphType>>("paymentMethodType")
                .Description("Value of payment method type")
                .Resolve(context => context.Source.PaymentMethodType.ToString());
            Field<NonNullGraphType<StringGraphType>>("paymentMethodGroupType")
                .Description("Value of payment group type")
                .Resolve(context => context.Source.PaymentMethodGroupType.ToString());
        }

        private static string GetLocalizedValue(IResolveFieldContext context, LocalizedString localizedString, string fallbackValue = null)
        {
            var cultureName = context.GetArgumentOrValue<string>("cultureName");

            if (!string.IsNullOrEmpty(cultureName))
            {
                var localizedValue = localizedString?.GetValue(cultureName);
                if (!string.IsNullOrEmpty(localizedValue))
                {
                    return localizedValue;
                }
            }

            return fallbackValue;
        }
    }
}
