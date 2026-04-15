using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using VirtoCommerce.CoreModule.Core.Currency;
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
}
