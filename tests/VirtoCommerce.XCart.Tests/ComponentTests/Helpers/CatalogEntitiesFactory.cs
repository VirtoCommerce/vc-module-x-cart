using System;
using VirtoCommerce.CatalogModule.Data.Model;
using VirtoCommerce.XCart.Tests.ComponentTests;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Helpers
{
    /// <summary>
    /// Factory for creating base VirtoCommerce catalog test entities (catalog, category, product).
    /// </summary>
    internal static class CatalogEntitiesFactory
    {
        public static CatalogEntity CreateCatalogEntity(string name, string id = TestConstants.CatalogId)
        {
            return new CatalogEntity
            {
                Id = id,
                Name = name,
                DefaultLanguage = TestConstants.LanguageCode,
            };
        }

        public static CategoryEntity CreateCategoryEntity(
            string id,
            string name,
            string code,
            string? parentCategoryId = null,
            bool isActive = true)
        {
            return new CategoryEntity
            {
                Id = id,
                CatalogId = TestConstants.CatalogId,
                Name = name,
                Code = code,
                ParentCategoryId = parentCategoryId,
                IsActive = isActive,
                StartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
        }

        public static ItemEntity CreateProductEntity(
            string id,
            string name,
            string code,
            string? categoryId = null,
            bool isActive = true,
            bool isBuyable = true,
            bool trackInventory = false)
        {
            return new ItemEntity
            {
                Id = id,
                CatalogId = TestConstants.CatalogId,
                CategoryId = categoryId,
                Name = name,
                Code = code,
                IsActive = isActive,
                IsBuyable = isBuyable,
                TrackInventory = trackInventory,
                // StartDate in the past so ProductIsAvailableSpecification treats the product as on sale.
                StartDate = DateTime.UtcNow.AddDays(-1),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
        }
    }
}
