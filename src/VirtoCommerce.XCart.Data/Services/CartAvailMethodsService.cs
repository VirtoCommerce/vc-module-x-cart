using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model.Search;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using StoreSetting = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartAvailMethodsService : ICartAvailMethodsService
    {
        private readonly IPaymentMethodsSearchService _paymentMethodsSearchService;
        private readonly ITaxProviderSearchService _taxProviderSearchService;
        private readonly IShippingMethodsSearchService _shippingMethodsSearchService;
        private readonly IGenericPipelineLauncher _pipeline;

        private readonly IMapper _mapper;

        private readonly int _takeOnSearch = 20;

        public CartAvailMethodsService(
            IPaymentMethodsSearchService paymentMethodsSearchService,
            IShippingMethodsSearchService shippingMethodsSearchService,
            ITaxProviderSearchService taxProviderSearchService,
            IMapper mapper,
            IGenericPipelineLauncher pipeline)
        {
            _paymentMethodsSearchService = paymentMethodsSearchService;
            _shippingMethodsSearchService = shippingMethodsSearchService;
            _taxProviderSearchService = taxProviderSearchService;
            _mapper = mapper;
            _pipeline = pipeline;
        }

        public async Task<IEnumerable<ShippingRate>> GetAvailableShippingRatesAsync(CartAggregate cartAggregate)
        {
            if (cartAggregate == null)
            {
                return Enumerable.Empty<ShippingRate>();
            }

            //Request available shipping rates
            var shippingEvaluationContext = new ShippingEvaluationContext(cartAggregate.Cart);

            var criteria = new ShippingMethodsSearchCriteria
            {
                IsActive = true,
                Take = _takeOnSearch,
                StoreId = cartAggregate.Store?.Id
            };

            var activeAvailableShippingMethods = (await _shippingMethodsSearchService.SearchAsync(criteria)).Results;

            var availableShippingRates = activeAvailableShippingMethods
                .SelectMany(x => x.CalculateRates(shippingEvaluationContext))
                .Where(x => x.ShippingMethod == null || x.ShippingMethod.IsActive)
                .ToArray();

            if (availableShippingRates.IsNullOrEmpty())
            {
                return Enumerable.Empty<ShippingRate>();
            }

            //Evaluate promotions cart and apply rewards for available shipping methods
            var evalContext = AbstractTypeFactory<PromotionEvaluationContext>.TryCreateInstance();
            var evalContextCartMap = new PromotionEvaluationContextCartMap
            {
                CartAggregate = cartAggregate,
                PromotionEvaluationContext = evalContext
            };
            await _pipeline.Execute(evalContextCartMap);

            var promoEvalResult = await cartAggregate.EvaluatePromotionsAsync(evalContextCartMap.PromotionEvaluationContext);
            foreach (var shippingRate in availableShippingRates)
            {
                shippingRate.ApplyRewards(promoEvalResult.Rewards);
            }

            var taxProvider = await GetActiveTaxProviderAsync(cartAggregate);
            if (taxProvider != null)
            {
                var taxEvalContext = _mapper.Map<TaxEvaluationContext>(cartAggregate);
                taxEvalContext.Lines.Clear();
                taxEvalContext.Lines.AddRange(availableShippingRates.SelectMany(x => _mapper.Map<IEnumerable<TaxLine>>(x)));

                var taxRates = taxProvider.CalculateRates(taxEvalContext).ToList();

                foreach (var shippingRate in availableShippingRates)
                {
                    shippingRate.ApplyTaxRates(taxRates);
                }
            }

            return availableShippingRates;
        }

        public async Task<IEnumerable<PaymentMethod>> GetAvailablePaymentMethodsAsync(CartAggregate cartAggregate)
        {
            if (cartAggregate == null)
            {
                return Enumerable.Empty<PaymentMethod>();
            }

            var criteria = new PaymentMethodsSearchCriteria
            {
                IsActive = true,
                Take = _takeOnSearch,
                StoreId = cartAggregate.Store?.Id,
            };

            var result = await _paymentMethodsSearchService.SearchAsync(criteria);
            if (result.Results.IsNullOrEmpty())
            {
                return Enumerable.Empty<PaymentMethod>();
            }

            var evalContext = AbstractTypeFactory<PromotionEvaluationContext>.TryCreateInstance();
            var evalContextCartMap = new PromotionEvaluationContextCartMap
            {
                CartAggregate = cartAggregate,
                PromotionEvaluationContext = evalContext
            };
            await _pipeline.Execute(evalContextCartMap);

            var promoResult = await cartAggregate.EvaluatePromotionsAsync(evalContextCartMap.PromotionEvaluationContext);

            foreach (var paymentMethod in result.Results)
            {
                paymentMethod.ApplyRewards(promoResult.Rewards);
            }

            //Evaluate taxes for available payments
            var taxProvider = await GetActiveTaxProviderAsync(cartAggregate);
            if (taxProvider != null)
            {
                var taxEvalContext = _mapper.Map<TaxEvaluationContext>(cartAggregate);
                taxEvalContext.Lines.Clear();
                taxEvalContext.Lines.AddRange(result.Results.SelectMany(x => _mapper.Map<IEnumerable<TaxLine>>(x)));

                var taxRates = taxProvider.CalculateRates(taxEvalContext).ToList();

                foreach (var paymentMethod in result.Results)
                {
                    paymentMethod.ApplyTaxRates(taxRates);
                }
            }

            return result.Results;
        }

        public async Task<IEnumerable<GiftItem>> GetAvailableGiftsAsync(CartAggregate cartAggregate)
        {
            var promotionEvalResult = await cartAggregate.EvaluatePromotionsAsync();

            return await cartAggregate.GetAvailableGiftsAsync(promotionEvalResult.Rewards);
        }

        protected async Task<TaxProvider> GetActiveTaxProviderAsync(string storeId)
        {
            var storeTaxProviders = await _taxProviderSearchService.SearchAsync(new TaxProviderSearchCriteria
            {
                StoreIds = [storeId]
            });

            return storeTaxProviders?.Results.FirstOrDefault(x => x.IsActive);
        }

        protected async Task<TaxProvider> GetActiveTaxProviderAsync(CartAggregate cartAggregate)
        {
            if (cartAggregate.Store?.Settings?.GetValue<bool>(StoreSetting.TaxCalculationEnabled) != true)
            {
                return null;
            }

            return await GetActiveTaxProviderAsync(cartAggregate.Store.Id);
        }
    }
}
