using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class GetSavedForLaterListQueryHandler(ISavedForLaterListService savedForLaterListService) : IQueryHandler<GetSavedForLaterListQuery, CartAggregate>
    {
        public Task<CartAggregate> Handle(GetSavedForLaterListQuery request, CancellationToken cancellationToken)
        {
            return savedForLaterListService.FindSavedForLaterListAsync(request);
        }
    }
}
