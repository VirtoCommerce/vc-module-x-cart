using AutoMapper;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class PaymentType : ExtendableGraphType<Payment>
    {
        public PaymentType(IMapper mapper, IMemberService memberService, IDataLoaderContextAccessor dataLoader, IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field(x => x.Id, nullable: false).Description("Payment Id");
            Field(x => x.OuterId, nullable: true).Description("Value of payment outer id");
            Field(x => x.PaymentGatewayCode, nullable: true).Description("Value of payment gateway code");
            Field(x => x.Purpose, nullable: true);
            Field<NonNullGraphType<CurrencyType>>("currency")
                .Description("Currency")
                .Resolve(context => context.GetCart().Currency);
            Field<NonNullGraphType<MoneyType>>("amount")
                .Description("Amount")
                .Resolve(context => context.Source.Amount.ToMoney(context.GetCart().Currency));
            ExtendableField<CartAddressType>("billingAddress",
                "Billing address",
                resolve: context => context.Source.BillingAddress);
            Field<NonNullGraphType<MoneyType>>("price")
                .Description("Price")
                .Resolve(context => context.Source.Price.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("priceWithTax")
                .Description("Price with tax")
                .Resolve(context => context.Source.PriceWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Total")
                .Resolve(context => context.Source.Total.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("totalWithTax")
                .Description("Total with tax")
                .Resolve(context => context.Source.TotalWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmount")
                .Description("Discount amount")
                .Resolve(context => context.Source.DiscountAmount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmountWithTax")
                .Description("Discount amount with tax")
                .Resolve(context => context.Source.DiscountAmountWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Tax total")
                .Resolve(context => context.Source.TaxTotal.ToMoney(context.GetCart().Currency));
            Field(x => x.TaxPercentRate, nullable: false).Description("Tax percent rate");
            Field(x => x.TaxType, nullable: true).Description("Tax type");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<TaxDetailType>>>>("taxDetails")
                .Description("Tax details")
                .Resolve(context => context.Source.TaxDetails);
            Field<NonNullGraphType<ListGraphType<DiscountType>>>("discounts")
                .Description("Discounts")
                .Resolve(context => context.Source.Discounts);
            Field(x => x.Comment, nullable: true).Description("Text comment");

            var vendorField = new FieldType
            {
                Name = "vendor",
                Type = GraphTypeExtensionHelper.GetActualType<VendorType>(),
                Resolver = new FuncFieldResolver<Payment, IDataLoaderResult<ExpVendor>>(context =>
                {
                    return dataLoader.LoadVendor(memberService, mapper, loaderKey: "cart_vendor", vendorId: context.Source.VendorId);
                })
            };
            AddField(vendorField);

            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Cart payment dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
