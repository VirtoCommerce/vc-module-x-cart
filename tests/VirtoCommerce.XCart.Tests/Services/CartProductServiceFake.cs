using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.InventoryModule.Core.Services;
using VirtoCommerce.PricingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Tests.Services
{
    public class CartProductServiceFake : CartProductService
    {
        public CartProductServiceFake(IItemService productService,
            IInventorySearchService inventoryService,
            IPricingEvaluatorService pricingEvaluatorService,
            IMapper mapper,
            ILoadUserToEvalContextService loadUserToEvalContextService,
            IMediator mediator)
            : base(productService, inventoryService, pricingEvaluatorService, mapper, loadUserToEvalContextService, mediator)
        {
        }

        internal Task<IList<CatalogProduct>> GetProductsByIdsFakeAsync(IList<string> ids)
        {
            return base.GetProductsByIdsAsync(ids);
        }
    }
}
