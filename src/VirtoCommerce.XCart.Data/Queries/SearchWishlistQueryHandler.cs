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

        public SearchWishlistQueryHandler(
            ICartAggregateRepository cartAggregateRepository,
            IMapper mapper,
            ISearchPhraseParser searchPhraseParser,
            ISavedForLaterListService savedForLaterListService)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _mapper = mapper;
            _searchPhraseParser = searchPhraseParser;
        }

        public virtual Task<SearchCartResponse> Handle(SearchWishlistQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = new CartSearchCriteriaBuilder(_searchPhraseParser, _mapper)
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

            return _cartAggregateRepository.SearchCartAsync(searchCriteria, request.IncludeFields.ItemsToProductIncludeField());
        }
    }
}
