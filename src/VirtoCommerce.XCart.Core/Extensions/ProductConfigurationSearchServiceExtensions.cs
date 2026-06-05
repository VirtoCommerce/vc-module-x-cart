using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class ProductConfigurationSearchServiceExtensions
{
    /// <summary>
    /// Enriches each request section with the catalog section name (<see cref="ProductConfigurationSection.SectionName"/>),
    /// looked up by <c>SectionId</c> against the product's active configuration. The name is a denormalized snapshot
    /// the aggregate stores onto the created <c>ConfigurationItem.SectionName</c>.
    /// </summary>
    public static async Task UpdateSectionsFromCatalogAsync(
        this IProductConfigurationSearchService searchService,
        string productId,
        IList<ProductConfigurationSection> sections)
    {
        if (string.IsNullOrEmpty(productId) || sections.IsNullOrEmpty())
        {
            return;
        }

        var searchCriteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
        searchCriteria.ProductId = productId;
        searchCriteria.IsActive = true;
        searchCriteria.Take = 1;

        var configuration = (await searchService.SearchNoCloneAsync(searchCriteria)).Results.FirstOrDefault();
        if (configuration is null || configuration.Sections.IsNullOrEmpty())
        {
            return;
        }

        var sectionNameById = configuration.Sections.ToDictionary(x => x.Id, x => x.Name);

        foreach (var section in sections)
        {
            if (sectionNameById.TryGetValue(section.SectionId, out var name))
            {
                section.SectionName = name;
            }
        }
    }
}
