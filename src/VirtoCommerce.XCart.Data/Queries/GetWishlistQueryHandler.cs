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
        private readonly ICartAggregateRepository _cartAggrRepository;

        public GetWishlistQueryHandler(ICartAggregateRepository cartAggrRepository)
        {
            _cartAggrRepository = cartAggrRepository;
        }

        public Task<CartAggregate> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            return _cartAggrRepository.GetCartByIdAsync(request.ListId, request.IncludeFields.ItemsToProductIncludeField(), language: request.CultureName);
        }
    }
}
