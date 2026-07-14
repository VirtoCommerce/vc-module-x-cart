using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartConfigurationService
    {
        /// <summary>
        /// Enriches each request configuration section with the catalog configuration section name
        /// (<see cref="ProductConfigurationSection.SectionName"/>), looked up by <c>SectionId</c> against the
        /// product's active configuration. The name is a denormalized snapshot the aggregate stores onto the
        /// created <c>ConfigurationItem.SectionName</c>. An empty catalog name is never written, so an existing
        /// snapshot is preserved — consistent with the Sku/ProductId/Name snapshot fields that survive catalog changes.
        /// </summary>
        Task UpdateSectionsFromCatalogAsync(string productId, IList<ProductConfigurationSection> configurationSections);
    }
}
