using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
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
        private readonly ISearchPhraseParser _searchPhraseParser;
        private readonly ICartResponseGroupParser _cartResponseGroupParser;

        public SearchCartQueryHandler(
            ICartAggregateRepository cartAggregateRepository,
            ISearchPhraseParser searchPhraseParser,
            ICartResponseGroupParser cartResponseGroupParser)
        {
            _cartAggregateRepository = cartAggregateRepository;
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
            return new CartSearchCriteriaBuilder(_searchPhraseParser)
                                     .ParseFilters(request.Filter)
                                     .WithCurrency(request.CurrencyCode)
                                     .WithStore(request.StoreId)
                                     .WithType(request.CartType)
                                     .WithLanguage(request.CultureName)
                                     .WithCustomerId(request.UserId)
                                     .WithOrganizationId(request.OrganizationId)
                                     .WithResponseGroup(EnumUtility.SafeParseFlags(_cartResponseGroupParser.GetResponseGroup(request.IncludeFields), CartResponseGroup.Full))
                                     .WithPaging(request.Skip, request.Take)
                                     .WithSorting(request.Sort)
                                     .Build();
        }
    }
}
