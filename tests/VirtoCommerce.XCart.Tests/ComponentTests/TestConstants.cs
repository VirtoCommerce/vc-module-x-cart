namespace VirtoCommerce.XCart.Tests.ComponentTests
{
    /// <summary>
    /// Shared constants used across X-Cart component tests.
    /// Using consistent IDs prevents issues with internal search operations that filter by StoreId/CatalogId.
    /// </summary>
    internal static class TestConstants
    {
        /// <summary>Default store ID for all component tests.</summary>
        public const string StoreId = "test-store";

        /// <summary>Default catalog ID for component tests.</summary>
        public const string CatalogId = "catalog-1";

        /// <summary>Default currency code for component tests.</summary>
        public const string Currency = "USD";

        /// <summary>Default language/culture code for component tests.</summary>
        public const string LanguageCode = "en-US";
    }
}
