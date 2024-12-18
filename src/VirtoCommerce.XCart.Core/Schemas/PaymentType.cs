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
            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.GetCart().Currency);
            Field<NonNullGraphType<MoneyType>>("amount",
                "Amount",
                resolve: context => context.Source.Amount.ToMoney(context.GetCart().Currency));
            ExtendableField<CartAddressType>("billingAddress",
                "Billing address",
                resolve: context => context.Source.BillingAddress);
            Field<NonNullGraphType<MoneyType>>("price",
                "Price",
                resolve: context => context.Source.Price.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("priceWithTax",
                "Price with tax",
                resolve: context => context.Source.PriceWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("total",
                "Total",
                resolve: context => context.Source.Total.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("totalWithTax",
                "Total with tax",
                resolve: context => context.Source.TotalWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmount",
                "Discount amount",
                resolve: context => context.Source.DiscountAmount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("discountAmountWithTax",
                "Discount amount with tax",
                resolve: context => context.Source.DiscountAmountWithTax.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("taxTotal",
                "Tax total",
                resolve: context => context.Source.TaxTotal.ToMoney(context.GetCart().Currency));
            Field(x => x.TaxPercentRate, nullable: false).Description("Tax percent rate");
            Field(x => x.TaxType, nullable: true).Description("Tax type");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<TaxDetailType>>>>("taxDetails",
                "Tax details",
                resolve: context => context.Source.TaxDetails);
            Field<NonNullGraphType<ListGraphType<DiscountType>>>("discounts",
                "Discounts",
                resolve: context => context.Source.Discounts);
            Field(x => x.Comment, nullable: true).Description("Text comment");

            var vendorField = new FieldType
            {
                Name = "vendor",
                Type = GraphTypeExtenstionHelper.GetActualType<VendorType>(),
                Resolver = new FuncFieldResolver<Payment, IDataLoaderResult<ExpVendor>>(context =>
                {
                    return dataLoader.LoadVendor(memberService, mapper, loaderKey: "cart_vendor", vendorId: context.Source.VendorId);
                })
            };
            AddField(vendorField);

            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Cart payment dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
