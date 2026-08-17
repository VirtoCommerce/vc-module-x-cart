using System;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Data.Middlewares
{
    public class MapPromoEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContextCartMap>
    {
        private readonly ILoadUserToEvalContextService _loadUserToEvalContextService;

        public MapPromoEvalContextMiddleware(ILoadUserToEvalContextService loadUserToEvalContextService)
        {
            _loadUserToEvalContextService = loadUserToEvalContextService;
        }

        public async Task Run(PromotionEvaluationContextCartMap parameter, Func<PromotionEvaluationContextCartMap, Task> next)
        {
            parameter.CartAggregate.MapTo(parameter.PromotionEvaluationContext);

            await _loadUserToEvalContextService.SetShopperDataFromMember(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.CustomerId);
            await _loadUserToEvalContextService.SetShopperDataFromOrganization(parameter.PromotionEvaluationContext, parameter.CartAggregate.Cart.OrganizationId);

            await next(parameter);
        }
    }
}
