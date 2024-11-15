using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCatalog.Data.Queries;

public class GetProductConfigurationQueryBuilder : QueryBuilder<GetProductConfigurationQuery, ProductConfigurationQueryResponse, ConfigurationQueryResponseType>
{
    protected override string Name => "productConfiguration";

    private readonly IStoreService _storeService;
    private readonly ICurrencyService _currencyService;

    public GetProductConfigurationQueryBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IStoreService storeService,
        ICurrencyService currencyService)
        : base(mediator, authorizationService)
    {
        _storeService = storeService;
        _currencyService = currencyService;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetProductConfigurationQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        request.IncludeFields = context.SubFields?.Values.GetAllNodesPaths(context).ToArray() ?? [];

        if (!string.IsNullOrEmpty(request.StoreId))
        {
            var store = await _storeService.GetByIdAsync(request.StoreId);
            request.Store = store;
            context.UserContext["store"] = store;
            context.UserContext["catalog"] = store.Catalog;
        }

        context.CopyArgumentsToUserContext();

        var currencies = await _currencyService.GetAllCurrenciesAsync();
        context.SetCurrencies(currencies, request.CultureName);
    }
}
