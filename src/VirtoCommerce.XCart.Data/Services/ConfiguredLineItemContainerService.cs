using System;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class ConfiguredLineItemContainerService : IConfiguredLineItemContainerService
{
    private readonly ICurrencyService _currencyService;
    private readonly IMemberResolver _memberResolver;
    private readonly IStoreService _storeService;

    public ConfiguredLineItemContainerService(
       ICurrencyService currencyService,
       IMemberResolver memberResolver,
       IStoreService storeService)
    {
        _currencyService = currencyService;
        _memberResolver = memberResolver;
        _storeService = storeService;
    }

    public async Task<ConfiguredLineItemContainer> CreateContainerAsync(ICartProductContainerRequest request)
    {
        var storeLoadTask = _storeService.GetByIdAsync(request.StoreId);
        var allCurrenciesLoadTask = _currencyService.GetAllCurrenciesAsync();
        await Task.WhenAll(storeLoadTask, allCurrenciesLoadTask);

        var store = storeLoadTask.Result;
        var allCurrencies = allCurrenciesLoadTask.Result;

        if (store == null)
        {
            throw new OperationCanceledException($"Store with id {request.StoreId} not found");
        }

        var language = !string.IsNullOrEmpty(request.CultureName) ? request.CultureName : store.DefaultLanguage;
        var currencyCode = !string.IsNullOrEmpty(request.CurrencyCode) ? request.CurrencyCode : store.DefaultCurrency;
        var currency = allCurrencies.GetCurrencyForLanguage(currencyCode, language);

        var member = await _memberResolver.ResolveMemberByIdAsync(request.UserId);

        var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();

        container.Store = store;
        container.Member = member;
        container.Currency = currency;
        container.CultureName = language;
        container.UserId = request.UserId;

        return container;
    }
}
