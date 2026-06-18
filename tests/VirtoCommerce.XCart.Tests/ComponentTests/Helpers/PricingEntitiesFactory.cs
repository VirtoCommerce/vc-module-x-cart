using System;
using VirtoCommerce.PricingModule.Data.Model;
using VirtoCommerce.XCart.Tests.ComponentTests;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Helpers
{
    /// <summary>
    /// Factory for creating base VirtoCommerce pricing test entities (pricelist, price, assignment).
    /// </summary>
    internal static class PricingEntitiesFactory
    {
        public static PricelistEntity CreatePricelistEntity(
            string name,
            string id = "default-pricelist")
        {
            return new PricelistEntity
            {
                Id = id,
                Name = name,
                Currency = TestConstants.Currency,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
        }

        public static PriceEntity CreatePriceEntity(
            string productId,
            string pricelistId,
            decimal listPrice,
            decimal? salePrice = null,
            int minQuantity = 1)
        {
            return new PriceEntity
            {
                Id = $"price-{pricelistId}-{productId}",
                ProductId = productId,
                PricelistId = pricelistId,
                List = listPrice,
                Sale = salePrice,
                MinQuantity = minQuantity,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
        }

        public static PricelistAssignmentEntity CreatePricelistAssignmentEntity(
            string pricelistId,
            string name)
        {
            return new PricelistAssignmentEntity
            {
                Id = $"assignment-{pricelistId}-{TestConstants.CatalogId}",
                PricelistId = pricelistId,
                CatalogId = TestConstants.CatalogId,
                Name = name,
                Priority = 1,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
        }
    }
}
