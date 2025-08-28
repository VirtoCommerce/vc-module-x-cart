using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class GetSharedWishlistQueryHandler(ICartSharingService cartSharingService) : IQueryHandler<GetSharedWishlistQuery, CartAggregate>
    {
        public async Task<CartAggregate> Handle(GetSharedWishlistQuery request, CancellationToken cancellationToken)
        {
            return await cartSharingService.GetWishlistBySharingKeyAsync(request.SharingKey, request.IncludeFields);
        }
    }
}
