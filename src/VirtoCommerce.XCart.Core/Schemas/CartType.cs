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
            Field<BooleanGraphType>("hasPhysicalProducts",
                "Has physical products",
                resolve: context => AbstractTypeFactory<CartHasPhysicalProductsSpecification>.TryCreateInstance().IsSatisfiedBy(context.Source.Cart));
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
            Field<NonNullGraphType<MoneyType>>("total",
                "Shopping cart total",
                resolve: context => context.GetTotal(context.Source.Cart.Total));
            Field<NonNullGraphType<MoneyType>>("subTotal",
                "Shopping cart subtotal",
                resolve: context => context.GetTotal(context.Source.Cart.SubTotal));
            Field<NonNullGraphType<MoneyType>>("subTotalWithTax",
                "Subtotal with tax",
                resolve: context => context.GetTotal(context.Source.Cart.SubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotal",
                "Total extended price",
                resolve: context => context.Source.SelectedLineItems.Sum(i => i.ExtendedPrice).ToMoney(context.Source.Currency));
            Field<NonNullGraphType<MoneyType>>("extendedPriceTotalWithTax",
                "Total extended price with tax",
                resolve: context => context.Source.SelectedLineItems.Sum(i => i.ExtendedPriceWithTax).ToMoney(context.Source.Currency));
            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.Source.Currency);
            Field<NonNullGraphType<MoneyType>>("taxTotal",
                "Total tax",
                resolve: context => context.GetTotal(context.Source.Cart.TaxTotal));
            Field(x => x.Cart.TaxPercentRate, nullable: false).Description("Tax percentage");
            Field(x => x.Cart.TaxType, nullable: false).Description("Shipping tax type");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<TaxDetailType>>>>("taxDetails",
                "Tax details",
                resolve: context => context.Source.Cart.TaxDetails);

            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.Fee).ToCamelCase(),
                "Shopping cart fee",
                resolve: context => context.GetTotal(context.Source.Cart.Fee));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeWithTax).ToCamelCase(),
                "Shopping cart fee with tax",
                resolve: context => context.GetTotal(context.Source.Cart.FeeWithTax));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeTotal).ToCamelCase(),
                "Total fee",
                resolve: context => context.GetTotal(context.Source.Cart.FeeTotal));
            Field<NonNullGraphType<MoneyType>>(nameof(ShoppingCart.FeeTotalWithTax).ToCamelCase(),
                "Total fee with tax",
                resolve: context => context.GetTotal(context.Source.Cart.FeeTotalWithTax));

            // Shipping
            Field<NonNullGraphType<MoneyType>>("shippingPrice",
                "Shipping price",
                resolve: context => context.GetTotal(context.Source.Cart.ShippingSubTotal));
            Field<NonNullGraphType<MoneyType>>("shippingPriceWithTax",
                "Shipping price with tax",
                resolve: context => context.GetTotal(context.Source.Cart.ShippingSubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("shippingTotal",
                "Total shipping",
                resolve: context => context.GetTotal(context.Source.Cart.ShippingTotal));
            Field<NonNullGraphType<MoneyType>>("shippingTotalWithTax",
                "Total shipping with tax",
                resolve: context => context.GetTotal(context.Source.Cart.ShippingTotalWithTax));
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
            Field<NonNullGraphType<MoneyType>>("paymentPrice",
                "Payment price",
                resolve: context => context.GetTotal(context.Source.Cart.PaymentSubTotal));
            Field<NonNullGraphType<MoneyType>>("paymentPriceWithTax",
                "Payment price with tax",
                resolve: context => context.GetTotal(context.Source.Cart.PaymentSubTotalWithTax));
            Field<NonNullGraphType<MoneyType>>("paymentTotal",
                "Total payment",
                resolve: context => context.GetTotal(context.Source.Cart.PaymentTotal));
            Field<NonNullGraphType<MoneyType>>("paymentTotalWithTax",
                "Total payment with tax",
                resolve: context => context.GetTotal(context.Source.Cart.PaymentTotalWithTax));
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
            Field<NonNullGraphType<MoneyType>>("handlingTotal",
                "Total handling",
                resolve: context => context.GetTotal(context.Source.Cart.HandlingTotal));
            Field<NonNullGraphType<MoneyType>>("handlingTotalWithTax",
                "Total handling with tax",
                resolve: context => context.GetTotal(context.Source.Cart.HandlingTotalWithTax));

            // Discounts
            Field<NonNullGraphType<MoneyType>>("discountTotal",
                "Total discount",
                resolve: context => context.GetTotal(context.Source.Cart.DiscountTotal));

            Field<NonNullGraphType<MoneyType>>("discountTotalWithTax",
                "Total discount with tax",
                resolve: context => context.GetTotal(context.Source.Cart.DiscountTotalWithTax));

            Field<NonNullGraphType<MoneyType>>("subTotalDiscount",
                "Subtotal discount",
                resolve: context => context.GetTotal(context.Source.Cart.SubTotalDiscount));

            Field<NonNullGraphType<MoneyType>>("subTotalDiscountWithTax",
                "Subtotal discount with tax",
                resolve: context => context.GetTotal(context.Source.Cart.SubTotalDiscountWithTax));

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<DiscountType>>>>("discounts",
                "Discounts",
                resolve: context => context.Source.Cart.Discounts);

            // Addresses
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CartAddressType>>>>("addresses",
                "Addresses",
                resolve: context => context.Source.Cart.Addresses);

            // Gifts
            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<GiftItemType>>>>("gifts", "Gifts", resolve: async context =>
            {
                var availableGifts = await cartAvailMethods.GetAvailableGiftsAsync(context.Source);
                return availableGifts.Where(x => x.LineItemId != null && x.LineItemSelectedForCheckout);
            });
            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<GiftItemType>>>>("availableGifts", "Available Gifts", resolve: async context =>
                await cartAvailMethods.GetAvailableGiftsAsync(context.Source)
            );

            // Items
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<LineItemType>>>>("items",
                "Items",
                resolve: context => context.Source.LineItems);

            Field<NonNullGraphType<IntGraphType>>("itemsCount",
                "Item count",
                resolve: context => context.Source.Cart.LineItemsCount);
            Field<NonNullGraphType<IntGraphType>>("itemsQuantity",
                "Quantity of items",
                resolve: context => context.Source.LineItems.Sum(x => x.Quantity));

            // Coupons
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<CouponType>>>>("coupons",
                "Coupons",
                resolve: context => context.Source.Coupons);

            // Other
            ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<DynamicPropertyValueType>>>>(
                "dynamicProperties",
                "Cart dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Cart, context.GetArgumentOrValue<string>("cultureName")));

            // Validation
            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<ValidationErrorType>>>>("validationErrors", "A set of errors in case the cart is invalid",
                QueryArgumentPresets.GetArgumentsForCartValidator(),
            resolve: async context =>
                {
                    var ruleSet = context.GetArgumentOrValue<string>("ruleSet");
                    await EnsureThatCartValidatedAsync(context.Source, cartValidationContextFactory, ruleSet);
                    return context.Source.GetValidationErrors().OfType<CartValidationError>();
                });

            Field(x => x.Cart.Type, nullable: true).Description("Shopping cart type");

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<ValidationErrorType>>>>("warnings", "A set of temporary warnings for a cart user", resolve: context => context.Source.ValidationWarnings);
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
