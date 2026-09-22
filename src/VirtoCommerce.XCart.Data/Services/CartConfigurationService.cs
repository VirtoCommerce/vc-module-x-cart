using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartConfigurationService : ICartConfigurationService
    {
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;

        public CartConfigurationService(IProductConfigurationSearchService productConfigurationSearchService)
        {
            _productConfigurationSearchService = productConfigurationSearchService;
        }

        public virtual async Task UpdateSectionsFromCatalogAsync(string productId, IList<ProductConfigurationSection> configurationSections)
        {
            if (string.IsNullOrEmpty(productId) || configurationSections.IsNullOrEmpty())
            {
                return;
            }

            var searchCriteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
            searchCriteria.ProductId = productId;
            searchCriteria.IsActive = true;
            searchCriteria.Take = 1;

            var configuration = (await _productConfigurationSearchService.SearchNoCloneAsync(searchCriteria)).Results.FirstOrDefault();
            if (configuration is null || configuration.Sections.IsNullOrEmpty())
            {
                return;
            }

            var sectionNameById = configuration.Sections.ToDictionary(x => x.Id, x => x.Name);

            foreach (var configurationSection in configurationSections)
            {
                // Never overwrite the snapshot with an empty name: a missing/blank catalog name preserves the
                // existing SectionName, mirroring the Sku/ProductId/Name snapshot fields that survive catalog changes.
                if (sectionNameById.TryGetValue(configurationSection.SectionId, out var name) && !string.IsNullOrEmpty(name))
                {
                    configurationSection.SectionName = name;
                }
            }
        }
    }
}
