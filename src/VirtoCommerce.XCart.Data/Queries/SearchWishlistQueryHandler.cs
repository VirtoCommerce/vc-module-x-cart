using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly ISearchPhraseParser _searchPhraseParser;
        private readonly ISavedForLaterListService _savedForLaterListService;

        public SearchWishlistQueryHandler(
            ICartAggregateRepository cartAggregateRepository,
            IMapper mapper,
            ISearchPhraseParser searchPhraseParser,
            ISavedForLaterListService savedForLaterListService)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _mapper = mapper;
            _searchPhraseParser = searchPhraseParser;
            _savedForLaterListService = savedForLaterListService;
        }

        public virtual async Task<SearchCartResponse> Handle(SearchWishlistQuery request, CancellationToken cancellationToken)
        {
            var wishlistSearchCriteria = new CartSearchCriteriaBuilder(_searchPhraseParser, _mapper)
                                     .WithCurrency(request.CurrencyCode)
                                     .WithStore(request.StoreId)
                                     .WithTypes([CartType.Wishlist])
                                     .WithLanguage(request.CultureName)
                                     .WithCustomerId(request.UserId)
                                     .WithOrganizationId(request.OrganizationId)
                                     .WithScope(request.Scope)
                                     .WithPaging(request.Skip, request.Take)
                                     .WithSorting(request.Sort)
                                     .WithResponseGroup(CartResponseGroup.WithLineItems)
                                     .Build();

            return await _cartAggregateRepository.SearchCartAsync(wishlistSearchCriteria, request.IncludeFields.ItemsToProductIncludeField());
        }
    }
}
