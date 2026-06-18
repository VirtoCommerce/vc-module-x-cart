using System.Linq;
using VirtoCommerce.PricingModule.Data.Repositories;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Helpers
{
    /// <summary>
    /// Helper for seeding pricing data into the test pricing database.
    /// </summary>
    internal static class PricingDataSeeder
    {
        /// <summary>
        /// Seeds a pricelist with $1 prices for each product, plus a pricelist assignment for the test catalog.
        /// </summary>
        public static void SeedDefaultPricing(
            PricingDbContext dbContext,
            params string[] productIds)
        {
            var pricelist = PricingEntitiesFactory.CreatePricelistEntity("Default Pricelist");

            dbContext.Add(pricelist);
            dbContext.SaveChanges();

            dbContext.AddRange(
                productIds.Select(id => PricingEntitiesFactory.CreatePriceEntity(id, pricelist.Id, listPrice: 1.00m)));
            dbContext.SaveChanges();

            dbContext.Add(PricingEntitiesFactory.CreatePricelistAssignmentEntity(pricelist.Id, "Default Assignment"));
            dbContext.SaveChanges();
        }

        /// <summary>
        /// Seeds a pricelist with per-product prices, plus a pricelist assignment for the test catalog.
        /// Each tuple is (productId, listPrice, salePrice).
        /// </summary>
        public static void SeedPricing(
            PricingDbContext dbContext,
            params (string ProductId, decimal ListPrice, decimal? SalePrice)[] prices)
        {
            var pricelist = PricingEntitiesFactory.CreatePricelistEntity("Default Pricelist");

            dbContext.Add(pricelist);
            dbContext.SaveChanges();

            dbContext.AddRange(
                prices.Select(x => PricingEntitiesFactory.CreatePriceEntity(x.ProductId, pricelist.Id, x.ListPrice, x.SalePrice)));
            dbContext.SaveChanges();

            dbContext.Add(PricingEntitiesFactory.CreatePricelistAssignmentEntity(pricelist.Id, "Default Assignment"));
            dbContext.SaveChanges();
        }
    }
}
