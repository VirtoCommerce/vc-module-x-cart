using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPricesSumQueryHandler : IQueryHandler<GetPricesSumQuery, ExpPricesSum>
{
    private readonly ICartProductsLoaderService _cartProductsLoaderService;
    private readonly ICurrencyService _currencyService;
    private readonly IMemberResolver _memberResolver;
    private readonly IStoreService _storeService;

    public GetPricesSumQueryHandler(
        ICartProductsLoaderService cartProductsLoaderService,
        ICurrencyService currencyService,
        IMemberResolver memberResolver,
        IStoreService storeService)
    {
        _cartProductsLoaderService = cartProductsLoaderService;
        _currencyService = currencyService;
        _memberResolver = memberResolver;
        _storeService = storeService;
    }

    public async Task<ExpPricesSum> Handle(GetPricesSumQuery request, CancellationToken cancellationToken)
    {
        var productRequest = await GetCartProductsRequest(request);
        var products = await _cartProductsLoaderService.GetCartProductsAsync(productRequest);

        var result = new ExpPricesSum
        {
            Currency = productRequest.Currency
        };
        foreach (var productPrice in products.Where(cartProduct => cartProduct.Price != null).Select(cartProduct => cartProduct.Price))
        {
            decimal salePrice = 0;
            decimal listPrice = 0;

            if (productPrice.ListPrice != null)
            {
                listPrice = productPrice.ListPrice.Amount;
            }

            if (productPrice.SalePrice != null)
            {
                salePrice = productPrice.SalePrice.Amount;
            }

            var discountAmount = Math.Max(0, listPrice - salePrice);
            result.ListPriceSum += listPrice;
            result.SalePriceSum += salePrice;
            result.DiscountAmountSum += discountAmount;
        }

        return result;
    }

    private async Task<CartProductsRequest> GetCartProductsRequest(GetPricesSumQuery request)
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

        var productRequest = AbstractTypeFactory<CartProductsRequest>.TryCreateInstance();

        productRequest.Store = store;
        productRequest.Member = member;
        productRequest.Currency = currency;
        productRequest.CultureName = language;
        productRequest.UserId = request.UserId;
        productRequest.ProductIds = request.ProductIds;
        productRequest.LoadInventory = false;

        return productRequest;
    }
}
