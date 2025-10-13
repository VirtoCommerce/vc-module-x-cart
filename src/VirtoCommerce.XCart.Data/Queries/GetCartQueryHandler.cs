using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Extensions;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class GetCartQueryHandler : IQueryHandler<GetCartQuery, CartAggregate>, IQueryHandler<GetCartByIdQuery, CartAggregate>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;
        private readonly ICartResponseGroupParser _cartResponseGroupParser;

        public GetCartQueryHandler(ICartAggregateRepository cartAggregateRepository, ICartResponseGroupParser cartResponseGroupParser)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _cartResponseGroupParser = cartResponseGroupParser;
        }

        public virtual Task<CartAggregate> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.CartId))
            {
                return _cartAggregateRepository.GetCartByIdAsync(request.CartId, GetResponseGroup(request), request.IncludeFields.ItemsToProductIncludeField(), request.CultureName);
            }

            var cartSearchCriteria = GetCartSearchCriteria(request);

            return _cartAggregateRepository.GetCartAsync(cartSearchCriteria, request.CultureName);
        }

        public virtual Task<CartAggregate> Handle(GetCartByIdQuery request, CancellationToken cancellationToken)
        {
            return _cartAggregateRepository.GetCartByIdAsync(request.CartId);
        }


        protected virtual ShoppingCartSearchCriteria GetCartSearchCriteria(GetCartQuery request)
        {
            var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

            cartSearchCriteria.StoreId = request.StoreId;
            cartSearchCriteria.CustomerId = request.UserId;
            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                cartSearchCriteria.OrganizationIdIsEmpty = true;
            }
            else
            {
                cartSearchCriteria.OrganizationId = request.OrganizationId;
            }
            cartSearchCriteria.Name = request.CartName;
            cartSearchCriteria.Currency = request.CurrencyCode;
            cartSearchCriteria.Type = request.CartType;
            cartSearchCriteria.ResponseGroup = GetResponseGroup(request);

            return cartSearchCriteria;
        }

        private string GetResponseGroup(GetCartQuery request)
        {
            return EnumUtility.SafeParseFlags(_cartResponseGroupParser.GetResponseGroup(request.IncludeFields), CartResponseGroup.Full).ToString();
        }
    }
}
