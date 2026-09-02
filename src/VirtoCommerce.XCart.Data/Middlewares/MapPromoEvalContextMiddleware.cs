using System;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Middlewares
{
    public class MapPromoEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContextCartMap>
    {
        private readonly ILoadUserToEvalContextService _loadUserToEvalContextService;
        private readonly IXCartMapper _mapper;

        public MapPromoEvalContextMiddleware(ILoadUserToEvalContextService loadUserToEvalContextService, IXCartMapper mapper)
        {
            _loadUserToEvalContextService = loadUserToEvalContextService;
            _mapper = mapper;
        }

        public async Task Run(PromotionEvaluationContextCartMap parameter, Func<PromotionEvaluationContextCartMap, Task> next)
        {
            _mapper.MapTo(parameter.CartAggregate, parameter.PromotionEvaluationContext);

            await _loadUserToEvalContextService.SetShopperDataFromMember(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.CustomerId);
            await _loadUserToEvalContextService.SetShopperDataFromOrganization(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.OrganizationId);

            await next(parameter);
        }
    }
}
