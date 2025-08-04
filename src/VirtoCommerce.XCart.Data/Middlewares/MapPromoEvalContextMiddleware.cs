using System;
using System.Threading.Tasks;
using AutoMapper;
using PipelineNet.Middleware;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Data.Middlewares
{
    public class MapPromoEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContextCartMap>
    {
        private readonly IMapper _mapper;
        private readonly ILoadUserToEvalContextService _loadUserToEvalContextService;

        public MapPromoEvalContextMiddleware(IMapper mapper, ILoadUserToEvalContextService loadUserToEvalContextService)
        {
            _mapper = mapper;
            _loadUserToEvalContextService = loadUserToEvalContextService;
        }

        public async Task Run(PromotionEvaluationContextCartMap parameter, Func<PromotionEvaluationContextCartMap, Task> next)
        {
            _mapper.Map(parameter.CartAggregate, parameter.PromotionEvaluationContext);

            await _loadUserToEvalContextService.SetShopperDataFromMember(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.CustomerId);
            await _loadUserToEvalContextService.SetShopperDataFromOrganization(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.OrganizationId);

            await next(parameter);
        }
    }
}
