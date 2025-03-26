using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Specifications;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartType : ExtendableGraphType<CartAggregate>
    {
        public CartType(
            ICartAvailMethodsService cartAvailMethods,
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            ICartValidationContextFactory cartValidationContextFactory)
        {
            Field(x => x.Cart.Id, nullable: false).Description("Shopping cart ID");
            Field(x => x.Cart.Name, nullable: false).Description("Shopping cart name");
            Field(x => x.Cart.Status, nullable: true).Description("Shopping cart status");
            Field(x => x.Cart.StoreId, nullable: false).Description("Shopping cart store ID");
            Field(x => x.Cart.ChannelId, nullable: true).Description("Shopping cart channel ID");
            Field<BooleanGraphType>("hasPhysicalProducts")
                .Description("Has physical products")
                .Resolve(context => AbstractTypeFactory<CartHasPhysicalProductsSpecification>.TryCreateInstance().IsSatisfiedBy(context.Source.Cart));
            Field(x => x.Cart.IsAnonymous, nullable: false).Description("Displays whether the shopping cart is anonymous");
            Field(x => x.Cart.CustomerId, nullable: false).Description("Shopping cart user ID");
            Field(x => x.Cart.CustomerName, nullable: true).Description("Shopping cart user name");
            Field(x => x.Cart.OrganizationId, nullable: true).Description("Shopping cart organization ID");
            Field(x => x.Cart.OrganizationName, nullable: true).Description("Shopping cart organization name");
            Field(x => x.Cart.IsRecuring, nullable: true).Description("Displays whether the shopping cart is recurring");
            Field(x => x.Cart.Comment, nullable: true).Description("Shopping cart text comment");
            Field(x => x.Cart.PurchaseOrderNumber, nullable: true).Description("Purchase order number");
            Field(x => x.Cart.CheckoutId, nullable: false).Description("Cart checkout ID");

            // Characteristics
            Field(x => x.Cart.VolumetricWeight, nullable: true).Description("Shopping cart volumetric weight value");
            Field(x => x.Cart.WeightUnit, nullable: true).Description("Shopping cart weight unit value");
            Field(x => x.Cart.Weight, nullable: true).Description("Shopping cart weight value");

            // Money
            Field<NonNullGraphType<MoneyType>>("total")
                .Description("Shopping cart total")
                .Resolve(context => context.GetTotal(context.Source.Cart.Total));
            Field<NonNullGraphType<MoneyType>>("subTotal")
                .Description("Shopping cart subtotal")
                .Resolve(context => context.GetTotal(context.Source.Cart.SubTotal));
            Field<NonNullGraphType<MoneyType>>("subTotalWithTax")
                .Description("Subtotal with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.SubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotal")
                .Description("Total extended price")
                .Resolve(context => context.Source.SelectedLineItems.Sum(i => i.ExtendedPrice).ToMoney(context.Source.Currency));
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotalWithTax")
                .Description("Total extended price with tax")
                .Resolve(context => context.Source.SelectedLineItems.Sum(i => i.ExtendedPriceWithTax).ToMoney(context.Source.Currency));
            Field<NonNullGraphType<CurrencyType>>("currency")
                .Description("Currency")
                .Resolve(context => context.Source.Currency);
            Field<NonNullGraphType<MoneyType>>("taxTotal")
                .Description("Total tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.TaxTotal));
            Field(x => x.Cart.TaxPercentRate, nullable: false).Description("Tax percentage");
            Field(x => x.Cart.TaxType, nullable: false).Description("Shipping tax type");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<TaxDetailType>>>>("taxDetails")
                .Description("Tax details")
                .Resolve(context => context.Source.Cart.TaxDetails);

            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.Fee).ToCamelCase())
                .Description("Shopping cart fee")
                .Resolve(context => context.GetTotal(context.Source.Cart.Fee));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeWithTax).ToCamelCase())
                .Description("Shopping cart fee with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.FeeWithTax));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeTotal).ToCamelCase())
                .Description("Total fee")
                .Resolve(context => context.GetTotal(context.Source.Cart.FeeTotal));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeTotalWithTax).ToCamelCase())
                .Description("Total fee with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.FeeTotalWithTax));

            // Shipping
            Field<NonNullGraphType<MoneyType>>("shippingPrice")
                .Description("Shipping price")
                .Resolve(context => context.GetTotal(context.Source.Cart.ShippingSubTotal));
            Field<NonNullGraphType<MoneyType>>("shippingPriceWithTax")
                .Description("Shipping price with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.ShippingSubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("shippingTotal")
                .Description("Total shipping")
                .Resolve(context => context.GetTotal(context.Source.Cart.ShippingTotal));
            Field<NonNullGraphType<MoneyType>>("shippingTotalWithTax")
                .Description("Total shipping with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.ShippingTotalWithTax));
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<ShipmentType>>>>("shipments",
                "Shipments",
                resolve: context => context.Source.Cart.Shipments);

            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<ShippingMethodType>>>>("availableShippingMethods", resolve: async context =>
            {
                var rates = await cartAvailMethods.GetAvailableShippingRatesAsync(context.Source);
                rates.Apply(x => context.UserContext[x.GetCacheKey()] = context.Source);
                return rates;
            });

            // Payment
            Field<NonNullGraphType<MoneyType>>("paymentPrice")
                .Description("Payment price")
                .Resolve(context => context.GetTotal(context.Source.Cart.PaymentSubTotal));
            Field<NonNullGraphType<MoneyType>>("paymentPriceWithTax")
                .Description("Payment price with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.PaymentSubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("paymentTotal")
                .Description("Total payment")
                .Resolve(context => context.GetTotal(context.Source.Cart.PaymentTotal));
            Field<NonNullGraphType<MoneyType>>("paymentTotalWithTax")
                .Description("Total payment with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.PaymentTotalWithTax));
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<PaymentType>>>>("payments",
                "Payments",
                resolve: context => context.Source.Cart.Payments);
            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<PaymentMethodType>>>>("availablePaymentMethods",
                "Available payment methods",
                resolve: async context =>
                {
                    var methods = await cartAvailMethods.GetAvailablePaymentMethodsAsync(context.Source);
                    //store the pair ShippingMethodType and cart aggregate in the user context for future usage in the ShippingMethodType fields resolvers
                    methods?.Apply(x => context.UserContext[x.Id] = context.Source);
                    return methods;
                });

            // Handling totals
            Field<NonNullGraphType<MoneyType>>("handlingTotal")
                .Description("Total handling")
                .Resolve(context => context.GetTotal(context.Source.Cart.HandlingTotal));
            Field<NonNullGraphType<MoneyType>>("handlingTotalWithTax")
                .Description("Total handling with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.HandlingTotalWithTax));

            // Discounts
            Field<NonNullGraphType<MoneyType>>("discountTotal")
                .Description("Total discount")
                .Resolve(context => context.GetTotal(context.Source.Cart.DiscountTotal));

            Field<NonNullGraphType<MoneyType>>("discountTotalWithTax")
                .Description("Total discount with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.DiscountTotalWithTax));

            Field<NonNullGraphType<MoneyType>>("subTotalDiscount")
                .Description("Subtotal discount")
                .Resolve(context => context.GetTotal(context.Source.Cart.SubTotalDiscount));

            Field<NonNullGraphType<MoneyType>>("subTotalDiscountWithTax")
                .Description("Subtotal discount with tax")
                .Resolve(context => context.GetTotal(context.Source.Cart.SubTotalDiscountWithTax));

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<DiscountType>>>>("discounts")
                .Description("Discounts")
                .Resolve(context => context.Source.Cart.Discounts);

            // Addresses
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CartAddressType>>>>("addresses",
                "Addresses",
                resolve: context => context.Source.Cart.Addresses);

            // Gifts
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GiftItemType>>>>("gifts").Description("Gifts").ResolveAsync(async context =>
            {
                var availableGifts = await cartAvailMethods.GetAvailableGiftsAsync(context.Source);
                return availableGifts.Where(x => x.LineItemId != null && x.LineItemSelectedForCheckout);
            });

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<GiftItemType>>>>("availableGifts").Description("Available Gifts").ResolveAsync(async context =>
                await cartAvailMethods.GetAvailableGiftsAsync(context.Source));

            // Items
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<LineItemType>>>>("items",
                "Items",
                resolve: context => context.Source.LineItems.OrderByDescending(x => x.CreatedDate));

            Field<NonNullGraphType<IntGraphType>>("itemsCount")
                .Description("Item count")
                .Resolve(context => context.Source.Cart.LineItemsCount);
            Field<NonNullGraphType<IntGraphType>>("itemsQuantity")
                .Description("Quantity of items")
                .Resolve(context => context.Source.LineItems.Sum(x => x.Quantity));

            // Coupons
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<CouponType>>>>("coupons")
                .Description("Coupons")
                .Resolve(context => context.Source.Coupons);

            // Other
            ExtendableFieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Cart dynamic property values",
                null,
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Cart, context.GetArgumentOrValue<string>("cultureName")));

            // Validation
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<ValidationErrorType>>>>("validationErrors")
                .Description("A set of errors in case the cart is invalid")
                .Arguments(QueryArgumentPresets.GetArgumentsForCartValidator())
                .ResolveAsync(async context =>
                {
                    var ruleSet = context.GetArgumentOrValue<string>("ruleSet");
                    await EnsureThatCartValidatedAsync(context.Source, cartValidationContextFactory, ruleSet);
                    return context.Source.GetValidationErrors().OfType<CartValidationError>();
                });

            Field(x => x.Cart.Type, nullable: true).Description("Shopping cart type");

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<ValidationErrorType>>>>("warnings")
                .Description("A set of temporary warnings for a cart user")
                .Resolve(context => context.Source.ValidationWarnings);
        }

        private static async Task EnsureThatCartValidatedAsync(CartAggregate cartAggr, ICartValidationContextFactory cartValidationContextFactory, string ruleSet)
        {
            if (!cartAggr.IsValidated)
            {
                var context = await cartValidationContextFactory.CreateValidationContextAsync(cartAggr);
                //We execute a cart validation only once and by demand, in order to do not introduce  performance issues with fetching data from external services
                //like shipping and tax rates etc.
                await cartAggr.ValidateAsync(context, ruleSet);
            }
        }
    }
}
