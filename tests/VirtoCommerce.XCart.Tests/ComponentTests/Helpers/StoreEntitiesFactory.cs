using VirtoCommerce.StoreModule.Data.Model;
using VirtoCommerce.XCart.Tests.ComponentTests;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Helpers
{
    /// <summary>
    /// Factory for creating base VirtoCommerce store test entities.
    /// </summary>
    internal static class StoreEntitiesFactory
    {
        /// <summary>
        /// Creates the default store entity used by component tests. Matches the TestConstants values
        /// for StoreId, CatalogId, Currency, and LanguageCode.
        /// </summary>
        public static StoreEntity CreateDefaultStoreEntity()
        {
            return new StoreEntity
            {
                Id = TestConstants.StoreId,
                Name = "Test Store",
                Catalog = TestConstants.CatalogId,
                DefaultCurrency = TestConstants.Currency,
                DefaultLanguage = TestConstants.LanguageCode,
                Languages =
                [
                    new StoreLanguageEntity { LanguageCode = TestConstants.LanguageCode },
                ],
            };
        }
    }
}
