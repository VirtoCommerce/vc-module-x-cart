using System;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Data.Middlewares
{
    public class LoadCartToEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContext>, IAsyncMiddleware<PriceEvaluationContext>, IAsyncMiddleware<TaxEvaluationContext>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;
        private readonly IGenericPipelineLauncher _pipeline;
        private readonly IXCartMapper _mapper;

        public LoadCartToEvalContextMiddleware(
            ICartAggregateRepository cartAggregateRepository,
            IGenericPipelineLauncher pipeline,
            IXCartMapper mapper)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _pipeline = pipeline;
            _mapper = mapper;
        }

        public async Task Run(PromotionEvaluationContext parameter, Func<PromotionEvaluationContext, Task> next)
        {
            var criteria = GetCartSearchCriteria(parameter);

            var cartAggregate = await _cartAggregateRepository.GetCartAsync(criteria, parameter.Language);
            if (cartAggregate != null)
            {
                var evalContextCartMap = AbstractTypeFactory<PromotionEvaluationContextCartMap>.TryCreateInstance();

                evalContextCartMap.CartAggregate = cartAggregate;
                evalContextCartMap.PromotionEvaluationContext = parameter;

                await _pipeline.Execute(evalContextCartMap);
            }

            await next(parameter);
        }

        public Task Run(PriceEvaluationContext parameter, Func<PriceEvaluationContext, Task> next)
        {
            return next(parameter);
        }

        public async Task Run(TaxEvaluationContext parameter, Func<TaxEvaluationContext, Task> next)
        {
            var criteria = GetCartSearchCriteria(parameter);

            var cartAggregate = await _cartAggregateRepository.GetCartAsync(criteria, Language.InvariantLanguage.CultureName);
            if (cartAggregate != null)
            {
                cartAggregate.MapTo(parameter, _mapper);
            }

            await next(parameter);
        }


        protected virtual ShoppingCartSearchCriteria GetCartSearchCriteria(PromotionEvaluationContext context)
        {
            var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

            cartSearchCriteria.Name = "default";
            cartSearchCriteria.StoreId = context.StoreId;
            cartSearchCriteria.CustomerId = context.CustomerId;
            cartSearchCriteria.OrganizationId = context.OrganizationId;
            cartSearchCriteria.OrganizationIdIsEmpty = string.IsNullOrEmpty(context.OrganizationId);
            cartSearchCriteria.Currency = context.Currency;
            cartSearchCriteria.ResponseGroup = CartResponseGroup.Full.ToString();

            return cartSearchCriteria;
        }

        protected virtual ShoppingCartSearchCriteria GetCartSearchCriteria(TaxEvaluationContext context)
        {
            var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

            cartSearchCriteria.Name = "default";
            cartSearchCriteria.StoreId = context.StoreId;
            cartSearchCriteria.CustomerId = context.CustomerId;
            cartSearchCriteria.OrganizationId = context.OrganizationId;
            cartSearchCriteria.OrganizationIdIsEmpty = string.IsNullOrEmpty(context.OrganizationId);
            cartSearchCriteria.Currency = context.Currency;
            cartSearchCriteria.ResponseGroup = CartResponseGroup.Full.ToString();

            return cartSearchCriteria;
        }
    }
}
