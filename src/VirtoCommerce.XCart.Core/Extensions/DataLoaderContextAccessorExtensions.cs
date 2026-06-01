using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);

    public static IDataLoader<string, ExpProduct> GetCartProductDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        ICurrencyService currencyService,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>(loaderKey, async ids =>
        {
            var cartAggregate = context.GetValueForSource<CartAggregate>();
            var cart = cartAggregate.Cart;
            var userId = context.GetArgumentOrValue<string>("userId") ?? cart.CustomerId;

            var request = new LoadProductsQuery
            {
                StoreId = cart.StoreId,
                CurrencyCode = cart.Currency,
                ObjectIds = ids.ToArray(),
                IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
                UserId = userId,
                OrganizationId = context.GetCurrentOrganizationId(),
            };

            var allCurrencies = await currencyService.GetAllCurrenciesAsync();
            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? cart.LanguageCode;
            context.SetCurrencies(allCurrencies, cultureName);
            context.UserContext.TryAdd("currencyCode", cart.Currency);
            context.UserContext.TryAdd("storeId", cart.StoreId);
            context.UserContext.TryAdd("store", cartAggregate.Store);
            context.UserContext.TryAdd("cultureName", cultureName);

            var response = await mediator.Send(request);

            return response.Products.ToDictionary(x => x.Id);
        });

        return loader;
    }

    public static IDataLoaderResult<ExpProduct> LoadCartProduct(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        ICurrencyService currencyService,
        string loaderKey,
        string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return _defaultProductResult;
        }

        var loader = dataLoader.GetCartProductDataLoader(context, mediator, currencyService, loaderKey);

        return loader.LoadAsync(productId);
    }

    public static IDataLoader<(string CurrencyCode, string ProductId), ExpProduct> GetCartCurrencyProductDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        ICurrencyService currencyService,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<(string CurrencyCode, string ProductId), ExpProduct>(loaderKey, async lineItems =>
        {
            var cartAggregate = context.GetValueForSource<CartAggregate>();
            var cart = cartAggregate.Cart;
            var userId = context.GetArgumentOrValue<string>("userId") ?? cart.CustomerId;
            var allCurrencies = await currencyService.GetAllCurrenciesAsync();
            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? cart.LanguageCode;

            context.SetCurrencies(allCurrencies, cultureName);
            context.UserContext.TryAdd("currencyCode", cart.Currency);
            context.UserContext.TryAdd("storeId", cart.StoreId);
            context.UserContext.TryAdd("store", cartAggregate.Store);
            context.UserContext.TryAdd("cultureName", cultureName);

            var lineItemsByCurrency = lineItems.GroupBy(x => x.CurrencyCode);
            var result = new Dictionary<(string CurrencyCode, string ProductId), ExpProduct>();

            foreach (var currencyLineItems in lineItemsByCurrency)
            {
                var request = new LoadProductsQuery
                {
                    StoreId = cart.StoreId,
                    CurrencyCode = currencyLineItems.Key,
                    ObjectIds = currencyLineItems.Select(x => x.ProductId).ToArray(),
                    IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray(),
                    UserId = userId,
                    OrganizationId = context.GetCurrentOrganizationId(),
                };

                var response = await mediator.Send(request);

                foreach (var item in currencyLineItems)
                {
                    var product = response.Products.FirstOrDefault(x => x.Id == item.ProductId);
                    if (product != null)
                    {
                        result.Add(item, product);
                    }
                }
            }

            return result;
        },
        keyComparer: AnonymousComparer.Create(((string CurrencyCode, string ProductId) x) => $"{x.ProductId}:{x.CurrencyCode}"));

        return loader;
    }

    public static IDataLoaderResult<ExpProduct> LoadCartProduct(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        ICurrencyService currencyService,
        string loaderKey,
        (string CurrencyCode, string ProductId) productCurrencyPair)
    {
        if (string.IsNullOrEmpty(productCurrencyPair.ProductId) || string.IsNullOrEmpty(productCurrencyPair.CurrencyCode))
        {
            return _defaultProductResult;
        }

        var loader = dataLoader.GetCartCurrencyProductDataLoader(context, mediator, currencyService, loaderKey);

        return loader.LoadAsync(productCurrencyPair);
    }
}
