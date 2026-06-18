using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.XCart.Tests.ComponentTests.Helpers;
using VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Cart
{
    /// <summary>
    /// Full-stack component test for the <c>cart</c> query: adds an item via the <c>addItem</c> mutation
    /// (persisting it to in-memory SQLite), then runs the <c>cart</c> query and asserts it reads back the
    /// persisted line item. End-to-end through the real GraphQL pipeline, Lucene index and SQLite.
    /// </summary>
    [Trait("Category", "ComponentTest")]
    public class GetCartQueryComponentTests
    {
        [Fact]
        public async Task Cart_ReadsPersistedLineItem()
        {
            // Arrange — seed store/catalog/category/product/price + index, then add an item.
            const string productId = "product-1";
            const decimal listPrice = 12.34m;

            using var ctx = CartTestContext.Create()
                .SeedStores(StoreEntitiesFactory.CreateDefaultStoreEntity())
                .SeedCatalogs(CatalogEntitiesFactory.CreateCatalogEntity("Test Catalog"))
                .SeedCategories(CatalogEntitiesFactory.CreateCategoryEntity("category-1", "Test Category", "CAT-1"))
                .SeedProducts(CatalogEntitiesFactory.CreateProductEntity(productId, "Test Product", "PROD-1", categoryId: "category-1"))
                .SeedPricing((productId, listPrice, null))
                .Build();

            await ctx.CreateIndexAsync();

            const string addItemMutation = """
                mutation($command: InputAddItemType!) {
                    addItem(command: $command) {
                        id
                        itemsCount
                    }
                }
                """;

            var addVariables = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, object>
                {
                    ["storeId"] = TestConstants.StoreId,
                    ["userId"] = "test-user",
                    ["currencyCode"] = TestConstants.Currency,
                    ["cultureName"] = TestConstants.LanguageCode,
                    ["productId"] = productId,
                    ["quantity"] = 1,
                },
            };

            var addResult = await ctx.ExecuteAsync(addItemMutation, addVariables);
            addResult.Errors.Should().BeNullOrEmpty("the item must be added before querying the cart");
            ((int)addResult.Data!["addItem"]!["itemsCount"]!).Should().Be(1);

            const string cartQuery = """
                query($storeId: String!, $currencyCode: String!, $userId: String) {
                    cart(storeId: $storeId, currencyCode: $currencyCode, userId: $userId) {
                        id
                        itemsCount
                        items {
                            productId
                            quantity
                        }
                    }
                }
                """;

            var queryVariables = new Dictionary<string, object>
            {
                ["storeId"] = TestConstants.StoreId,
                ["currencyCode"] = TestConstants.Currency,
                ["userId"] = "test-user",
            };

            // Act
            var result = await ctx.ExecuteAsync(cartQuery, queryVariables);

            // Assert — the query reads the persisted cart state.
            result.Errors.Should().BeNullOrEmpty();
            result.Data.Should().NotBeNull();

            var cart = result.Data!["cart"]!;
            ((int)cart["itemsCount"]!).Should().Be(1);

            var items = cart["items"]!;
            items.Should().HaveCount(1);
            ((string)items[0]!["productId"]!).Should().Be(productId);
            ((int)items[0]!["quantity"]!).Should().Be(1);
        }
    }
}
