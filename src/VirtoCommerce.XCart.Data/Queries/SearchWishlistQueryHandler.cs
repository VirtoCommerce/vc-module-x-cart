using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Extensions;
using VirtoCommerce.XCart.Data.Services;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class SearchWishlistQueryHandler : IQueryHandler<SearchWishlistQuery, SearchCartResponse>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;
        private readonly ISearchPhraseParser _searchPhraseParser;
        private readonly IXCartMapper _mapper;
        private readonly ICartSharingService _cartSharingService;

        public SearchWishlistQueryHandler(
            ICartAggregateRepository cartAggregateRepository,
            ISearchPhraseParser searchPhraseParser,
            ISavedForLaterListService savedForLaterListService,
            IXCartMapper mapper,
            ICartSharingService cartSharingService)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _searchPhraseParser = searchPhraseParser;
            _mapper = mapper;
            _cartSharingService = cartSharingService;
        }

        public virtual Task<SearchCartResponse> Handle(SearchWishlistQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = new CartSearchCriteriaBuilder(_searchPhraseParser, _mapper, _cartSharingService)
                                     .WithCurrency(request.CurrencyCode)
                                     .WithStore(request.StoreId)
                                     .WithTypes([CartType.Wishlist])
                                     .WithLanguage(request.CultureName)
                                     .WithCustomerId(request.UserId)
                                     .WithOrganizationId(request.OrganizationId)
                                     .WithScope(request.Scope)
                                     .WithPaging(request.Skip, request.Take)
                                     .WithSorting(request.Sort)
                                     .WithResponseGroup(GetResponseGroup(request))
                                     .Build();

            return _cartAggregateRepository.SearchCartAsync(searchCriteria, request.IncludeFields.ItemsToProductIncludeField());
        }

        // A rep may share one list with a thousand organizations, and each is a row: a page of lists loads the
        // recipients only when the query actually selects them.
        protected virtual CartResponseGroup GetResponseGroup(SearchWishlistQuery request)
        {
            var result = CartResponseGroup.WithLineItems;

            if (request.IncludeFields?.Any(x => x.Contains("targets", StringComparison.OrdinalIgnoreCase) ||
                                                x.Contains("sharedWithId", StringComparison.OrdinalIgnoreCase)) == true)
            {
                result |= CartResponseGroup.WithSharingTargets;
            }

            return result;
        }
    }
}
