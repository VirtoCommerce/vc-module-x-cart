using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class CartValidationContextFactory : ICartValidationContextFactory
    {
        private readonly ICartAvailMethodsService _availMethods;
        private readonly ICartProductService _cartProducts;

        public CartValidationContextFactory(ICartAvailMethodsService availMethods, ICartProductService cartProducts)
        {
            _availMethods = availMethods;
            _cartProducts = cartProducts;
        }

        public async Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate)
        {
            var availPaymentsTask = _availMethods.GetAvailablePaymentMethodsAsync(cartAggregate);
            var availShippingRatesTask = _availMethods.GetAvailableShippingRatesAsync(cartAggregate);
            var cartProductsTask = _cartProducts.GetCartProductsAsync(cartAggregate, cartAggregate.Cart.Items.Select(x => (x.Currency, x.ProductId)).ToList());
            await Task.WhenAll(availPaymentsTask, availShippingRatesTask, cartProductsTask);

            var context = AbstractTypeFactory<CartValidationContext>.TryCreateInstance();
            context.CartAggregate = cartAggregate;
            context.AllCartProducts = cartProductsTask.Result.Values;
            context.AvailPaymentMethods = availPaymentsTask.Result;
            context.AvailShippingRates = availShippingRatesTask.Result;
            return context;
        }

        public async Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate, IList<CartProduct> products)
        {
            var availPaymentsTask = _availMethods.GetAvailablePaymentMethodsAsync(cartAggregate);
            var availShippingRatesTask = _availMethods.GetAvailableShippingRatesAsync(cartAggregate);
            await Task.WhenAll(availPaymentsTask, availShippingRatesTask);

            return new CartValidationContext
            {
                CartAggregate = cartAggregate,
                AllCartProducts = products,
                AvailPaymentMethods = availPaymentsTask.Result,
                AvailShippingRates = availShippingRatesTask.Result,
            };
        }
    }
}
