using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Extensions;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class GetWishlistQueryHandler : IQueryHandler<GetWishlistQuery, CartAggregate>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;

        public GetWishlistQueryHandler(ICartAggregateRepository cartAggregateRepository)
        {
            _cartAggregateRepository = cartAggregateRepository;
        }

        public Task<CartAggregate> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            return _cartAggregateRepository.GetCartByIdAsync(request.ListId, request.IncludeFields.ItemsToProductIncludeField(), request.CultureName);
        }
    }
}
