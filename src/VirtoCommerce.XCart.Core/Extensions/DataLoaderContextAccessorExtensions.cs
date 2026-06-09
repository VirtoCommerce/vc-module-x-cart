using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    private static readonly DataLoaderResult<ExpProduct> _defaultProductResult = new((ExpProduct)null);
    private static readonly DataLoaderResult<ExpProductConfigurationSection> _defaultConfigurationSectionResult = new((ExpProductConfigurationSection)null);

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

    public static IDataLoader<string, ExpProductConfigurationSection> GetProductConfigurationSectionDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProductConfigurationSection>(loaderKey, async sectionIds =>
        {
            var cartAggregate = context.GetValueForSource<CartAggregate>();
            var cart = cartAggregate.Cart;

            // The requested sub-fields drive the catalog response group (sections only / + options / full),
            // so we load no more configuration data than the client asked for.
            var includeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
            var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? cart.LanguageCode;
            var userId = context.GetArgumentOrValue<string>("userId") ?? cart.CustomerId;
            var organizationId = context.GetCurrentOrganizationId();

            // A section can only be fetched through its product's configuration, so map each requested
            // section to its owning configurable product from the cart's configured line items.
            var productIdBySectionId = cart.Items
                .Where(lineItem => lineItem.ConfigurationItems != null)
                .SelectMany(lineItem => lineItem.ConfigurationItems.Select(item => new { item.SectionId, lineItem.ProductId }))
                .GroupBy(x => x.SectionId)
                .ToDictionary(x => x.Key, x => x.First().ProductId);

            var result = new Dictionary<string, ExpProductConfigurationSection>();

            // One query per configurable product, requesting only the sections referenced by this cart's items.
            var groups = sectionIds
                .Where(productIdBySectionId.ContainsKey)
                .GroupBy(sectionId => productIdBySectionId[sectionId]);

            foreach (var group in groups)
            {
                var request = new GetProductConfigurationQuery
                {
                    ConfigurableProductId = group.Key,
                    SectionIds = group.Distinct().ToArray(),
                    StoreId = cart.StoreId,
                    CurrencyCode = cart.Currency,
                    CultureName = cultureName,
                    UserId = userId,
                    OrganizationId = organizationId,
                    IncludeFields = includeFields,
                };

                var response = await mediator.Send(request);

                foreach (var section in response.ConfigurationSections)
                {
                    result[section.Id] = section;
                }
            }

            return result;
        });

        return loader;
    }

    public static IDataLoaderResult<ExpProductConfigurationSection> LoadConfigurationSection(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        IMediator mediator,
        string loaderKey,
        string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId))
        {
            return _defaultConfigurationSectionResult;
        }

        var loader = dataLoader.GetProductConfigurationSectionDataLoader(context, mediator, loaderKey);

        return loader.LoadAsync(sectionId);
    }
}
