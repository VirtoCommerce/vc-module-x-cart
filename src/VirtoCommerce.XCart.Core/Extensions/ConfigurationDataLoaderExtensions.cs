using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class ConfigurationDataLoaderExtensions
{
    private static readonly DataLoaderResult<ExpProductConfigurationSection> _defaultConfigurationSectionResult = new((ExpProductConfigurationSection)null);

    public static IDataLoader<string, ExpProductConfigurationSection> GetProductConfigurationSectionDataLoader(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext context,
        string loaderKey)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProductConfigurationSection>(loaderKey, async sectionIds =>
        {
            var mediator = context.GetMediator();

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
        string loaderKey,
        string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId))
        {
            return _defaultConfigurationSectionResult;
        }

        var loader = dataLoader.GetProductConfigurationSectionDataLoader(context, loaderKey);

        return loader.LoadAsync(sectionId);
    }
}
