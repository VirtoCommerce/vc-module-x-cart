using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class SearchCartQueryHandler : IQueryHandler<SearchCartQuery, SearchCartResponse>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;
        private readonly IMapper _mapper;
        private readonly ISearchPhraseParser _searchPhraseParser;
        private readonly ICartResponseGroupParser _cartResponseGroupParser;

        public SearchCartQueryHandler(
            ICartAggregateRepository cartAggregateRepository,
            IMapper mapper,
            ISearchPhraseParser searchPhraseParser,
            ICartResponseGroupParser cartResponseGroupParser)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _mapper = mapper;
            _searchPhraseParser = searchPhraseParser;
            _cartResponseGroupParser = cartResponseGroupParser;
        }

        public virtual Task<SearchCartResponse> Handle(SearchCartQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = GetSearchCriteria(request);

            return _cartAggregateRepository.SearchCartAsync(searchCriteria);
        }

        protected virtual ShoppingCartSearchCriteria GetSearchCriteria(SearchCartQuery request)
        {
            return new CartSearchCriteriaBuilder(_searchPhraseParser, _mapper)
                                     .ParseFilters(request.Filter)
                                     .WithCurrency(request.CurrencyCode)
                                     .WithStore(request.StoreId)
                                     .WithType(request.CartType)
                                     .WithLanguage(request.CultureName)
                                     .WithCustomerId(request.UserId)
                                     .WithOrganizationId(request.OrganizationId)
                                     .WithXCartResponseGroup()
                                     .WithPaging(request.Skip, request.Take)
                                     .WithSorting(request.Sort)
                                     .Build();
        }
    }
}
