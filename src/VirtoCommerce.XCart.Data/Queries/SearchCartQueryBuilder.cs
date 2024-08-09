using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Queries
{
    public class SearchCartQueryBuilder : SearchQueryBuilder<SearchCartQuery, SearchCartResponse, CartAggregate, CartType>
    {
        protected override string Name => "carts";

        private readonly ICurrencyService _currencyService;
        private readonly IUserManagerCore _userManagerCore;

        public SearchCartQueryBuilder(
            IMediator mediator,
            IAuthorizationService authorizationService,
            ICurrencyService currencyService,
            IUserManagerCore userManagerCore)
            : base(mediator, authorizationService)
        {
            _currencyService = currencyService;
            _userManagerCore = userManagerCore;
        }

        protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, SearchCartQuery request)
        {
            context.CopyArgumentsToUserContext();

            var allCurrencies = await _currencyService.GetAllCurrenciesAsync();
            //Store all currencies in the user context for future resolve in the schema types
            //this is required to resolve Currency in DiscountType
            context.SetCurrencies(allCurrencies, request.CultureName);

            await Authorize(context, request, new CanAccessCartAuthorizationRequirement());
        }

        protected override Task AfterMediatorSend(IResolveFieldContext<object> context, SearchCartQuery request, SearchCartResponse response)
        {
            foreach (var cartAggregate in response.Results)
            {
                context.SetExpandedObjectGraph(cartAggregate);
            }

            return Task.CompletedTask;
        }

        protected override async Task Authorize(IResolveFieldContext context, object resource, IAuthorizationRequirement requirement)
        {
            await _userManagerCore.CheckCurrentUserState(context, allowAnonymous: true);

            await base.Authorize(context, resource, requirement);
        }
    }
}
