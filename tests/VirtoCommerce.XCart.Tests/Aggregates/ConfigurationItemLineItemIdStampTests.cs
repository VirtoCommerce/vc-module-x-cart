using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Aggregates
{
    /// <summary>
    /// Regression coverage for the broadened VCST-5391 fix.
    ///
    /// Only the <c>changeCartConfiguredItem</c> (edit) path originally stamped
    /// <see cref="ConfigurationItem.LineItemId"/>. The add-to-cart
    /// (<c>AddCartItemCommandHandler.AddItemToCartAsync</c> → <c>CartAggregate.AddConfiguredItemAsync</c>)
    /// and move-from-saved-for-later (<c>SavedForLaterListService.MoveItemsAsync</c> →
    /// <c>CartAggregate.AddConfiguredItemAsync</c>) paths add the configured line item with
    /// <c>Id = null</c> and never set the configuration items' <c>LineItemId</c> back-reference.
    ///
    /// On the GraphQL mutation return path the <c>CartConfigurationItemType.extendedPrice</c> money resolver
    /// looks up the owning line item (to obtain its currency) by matching
    /// <c>ConfigurationItem.LineItemId == cart.Cart.Items[].Id</c>
    /// (<c>ResolveFieldContextExtensions.GetConfiguratonItemCurrency</c>). With the back-reference unset the
    /// currency resolves to null and <c>ExtendedPrice.ToMoney(null)</c> throws ARGUMENT_NULL — the same
    /// <c>errors[]</c> + <c>configurationItems:[null]</c> symptom as the edit path.
    ///
    /// The fix stamps <c>LineItemId</c> centrally for EVERY configured line item via
    /// <see cref="CartAggregate.SetConfigurationItemsLineItemId()"/>, invoked from
    /// <c>CartAggregateRepository.SaveAsync</c> once persistence has assigned the real line item ids.
    /// This test mirrors the resolver's key against the post-save aggregate state for the add/move paths.
    /// </summary>
    public class ConfigurationItemLineItemIdStampTests : XCartMoqHelper
    {
        [Fact]
        public void SetConfigurationItemsLineItemId_StampsEveryConfiguredLineItem()
        {
            // Arrange — two configured line items whose configuration items carry NO LineItemId
            // back-reference, exactly as produced by the add-to-cart / move-from-saved-for-later paths
            // (AddConfiguredItemAsync sets Id=null; persistence then assigns the real id captured below).
            var cartAggregate = GetValidCartAggregate();

            var configuredItemA = new LineItem
            {
                Id = "li-A",
                ProductId = "configurable-1",
                IsConfigured = true,
                Currency = "USD",
                ConfigurationItems =
                [
                    new() { Id = "cfg-A1", Type = ConfigurationSectionTypeProduct, SectionId = "sec-1", ProductId = "p-1" },
                    new() { Id = "cfg-A2", Type = ConfigurationSectionTypeText, SectionId = "sec-2", CustomText = "engraving" },
                ],
            };

            var configuredItemB = new LineItem
            {
                Id = "li-B",
                ProductId = "configurable-2",
                IsConfigured = true,
                Currency = "USD",
                ConfigurationItems =
                [
                    new() { Id = "cfg-B1", Type = ConfigurationSectionTypeProduct, SectionId = "sec-1", ProductId = "p-2" },
                ],
            };

            // A plain (non-configured) item with no configuration items must be left untouched.
            var ordinaryItem = new LineItem { Id = "li-C", ProductId = "plain", IsConfigured = false };

            cartAggregate.Cart.Items = new List<LineItem> { configuredItemA, configuredItemB, ordinaryItem };

            // Pre-condition: the back-reference is missing on the freshly built configuration items
            // (this is the broken state that reaches the resolver on the add/move paths).
            cartAggregate.Cart.Items
                .SelectMany(x => x.ConfigurationItems ?? new List<ConfigurationItem>())
                .Should().OnlyContain(c => c.LineItemId == null);

            // Act
            cartAggregate.SetConfigurationItemsLineItemId();

            // Assert — every configuration item now keys back to its owning line item's id, so the
            // GetConfiguratonItemCurrency lookup (LineItemId == lineItem.Id) succeeds and extendedPrice
            // no longer throws ARGUMENT_NULL.
            cartAggregate.Cart.Items.Single(x => x.Id == "li-A").ConfigurationItems
                .Should().OnlyContain(c => c.LineItemId == "li-A");
            cartAggregate.Cart.Items.Single(x => x.Id == "li-B").ConfigurationItems
                .Should().OnlyContain(c => c.LineItemId == "li-B");

            // Every configuration item across the cart now resolves against an existing line item id.
            var lineItemIds = cartAggregate.Cart.Items.Select(x => x.Id).ToHashSet();
            cartAggregate.Cart.Items
                .SelectMany(x => x.ConfigurationItems ?? new List<ConfigurationItem>())
                .Should().OnlyContain(c => lineItemIds.Contains(c.LineItemId));
        }
    }
}
