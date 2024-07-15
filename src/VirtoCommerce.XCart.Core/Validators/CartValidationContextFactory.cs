using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var cartProductsTask = _cartProducts.GetCartProductsByIdsAsync(cartAggregate, cartAggregate.Cart.Items.Select(x => x.ProductId).ToArray());
            await Task.WhenAll(availPaymentsTask, availShippingRatesTask, cartProductsTask);

            return new CartValidationContext
            {
                CartAggregate = cartAggregate,
                AllCartProducts = cartProductsTask.Result,
                AvailPaymentMethods = availPaymentsTask.Result,
                AvailShippingRates = availShippingRatesTask.Result,
            };
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
