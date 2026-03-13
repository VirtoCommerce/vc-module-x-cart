using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class CartValidationContextFactory : ICartValidationContextFactory
    {
        private readonly ICartAvailMethodsService _availMethods;
        private readonly ICartProductService _cartProducts;
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;

        public CartValidationContextFactory(
            ICartAvailMethodsService availMethods,
            ICartProductService cartProducts,
            IProductConfigurationSearchService productConfigurationSearchService)
        {
            _availMethods = availMethods;
            _cartProducts = cartProducts;
            _productConfigurationSearchService = productConfigurationSearchService;
        }

        public async Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate)
        {
            var availPaymentsTask = _availMethods.GetAvailablePaymentMethodsAsync(cartAggregate);
            var availShippingRatesTask = _availMethods.GetAvailableShippingRatesAsync(cartAggregate);
            var cartProductsTask = _cartProducts.GetCartProductsByIdsAsync(cartAggregate, cartAggregate.Cart.Items.Select(x => x.ProductId).ToArray());
            var configurationsTask = LoadProductConfigurationsAsync(cartAggregate);
            await Task.WhenAll(availPaymentsTask, availShippingRatesTask, cartProductsTask, configurationsTask);

            return new CartValidationContext
            {
                CartAggregate = cartAggregate,
                AllCartProducts = cartProductsTask.Result,
                AvailPaymentMethods = availPaymentsTask.Result,
                AvailShippingRates = availShippingRatesTask.Result,
                ProductConfigurations = configurationsTask.Result,
            };
        }

        public async Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate, IList<CartProduct> products)
        {
            var availPaymentsTask = _availMethods.GetAvailablePaymentMethodsAsync(cartAggregate);
            var availShippingRatesTask = _availMethods.GetAvailableShippingRatesAsync(cartAggregate);
            var configurationsTask = LoadProductConfigurationsAsync(cartAggregate);
            await Task.WhenAll(availPaymentsTask, availShippingRatesTask, configurationsTask);

            return new CartValidationContext
            {
                CartAggregate = cartAggregate,
                AllCartProducts = products,
                AvailPaymentMethods = availPaymentsTask.Result,
                AvailShippingRates = availShippingRatesTask.Result,
                ProductConfigurations = configurationsTask.Result,
            };
        }

        protected virtual async Task<IDictionary<string, ProductConfiguration>> LoadProductConfigurationsAsync(CartAggregate cartAggregate)
        {
            var configuredProductIds = cartAggregate.Cart.Items
                .Where(x => x.IsConfigured)
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            if (configuredProductIds.Length == 0)
            {
                return null;
            }

            var criteria = new ProductConfigurationSearchCriteria
            {
                ProductIds = configuredProductIds,
                IsActive = true,
            };

            var configurations = await _productConfigurationSearchService.SearchAllNoCloneAsync(criteria);

            return configurations.DistinctBy(x => x.ProductId).ToDictionary(x => x.ProductId);
        }
    }
}
