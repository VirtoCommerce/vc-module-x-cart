using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.XCart.Tests.ComponentTests.Helpers;
using VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure;
using Xunit;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Cart
{
    /// <summary>
    /// Full-stack component test for the <c>addItem</c> mutation: seeds a store, catalog, category,
    /// an active+buyable product and a price into in-memory SQLite, builds the in-memory Lucene index,
    /// then runs the mutation through the real GraphQL pipeline. Proves the product was resolved through
    /// the Lucene index + pricing pipeline and persisted to SQLite.
    /// </summary>
    [Trait("Category", "ComponentTest")]
    public class AddItemMutationComponentTests
    {
        [Fact]
        public async Task AddItem_ResolvesProductThroughSearchAndPricing_AndPersistsLineItem()
        {
            // Arrange
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

            const string mutation = """
                mutation($command: InputAddItemType!) {
                    addItem(command: $command) {
                        id
                        itemsCount
                        items {
                            productId
                            quantity
                            listPrice { amount }
                            extendedPrice { amount }
                        }
                    }
                }
                """;

            var variables = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, object>
                {
                    ["storeId"] = TestConstants.StoreId,
                    ["userId"] = "test-user",
                    ["currencyCode"] = TestConstants.Currency,
                    ["cultureName"] = TestConstants.LanguageCode,
                    ["productId"] = productId,
                    ["quantity"] = 2,
                },
            };

            // Act
            var result = await ctx.ExecuteAsync(mutation, variables);

            // Assert
            result.Errors.Should().BeNullOrEmpty();
            result.Data.Should().NotBeNull();

            var cart = result.Data!["addItem"]!;
            ((int)cart["itemsCount"]!).Should().Be(1);

            var items = cart["items"]!;
            items.Should().HaveCount(1);

            var item = items[0]!;
            ((string)item["productId"]!).Should().Be(productId);
            ((int)item["quantity"]!).Should().Be(2);
            ((decimal)item["listPrice"]!["amount"]!).Should().Be(listPrice);
            // ExtendedPrice = listPrice * quantity (no discounts/taxes in the harness).
            ((decimal)item["extendedPrice"]!["amount"]!).Should().Be(listPrice * 2);
        }
    }
}
