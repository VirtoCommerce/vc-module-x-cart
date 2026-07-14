using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Aggregates
{
    /// <summary>
    /// Regression test for VCST-5391.
    ///
    /// The <c>changeCartConfiguredItem</c> GraphQL mutation returned HTTP 200 with a top-level
    /// <c>errors[]</c> ("Error trying to resolve field 'extendedPrice'", code ARGUMENT_NULL) on
    /// <c>items[].configurationItems[].extendedPrice</c>, nulling the parent configuration item.
    ///
    /// Root cause: <see cref="CartAggregate.UpdateConfiguredLineItemAsync"/> assigns the freshly built
    /// configuration items onto the line item WITHOUT setting their <see cref="ConfigurationItem.LineItemId"/>
    /// back-reference. The <c>CartConfigurationItemType.extendedPrice</c> money resolver looks up the
    /// owning line item (to obtain its currency) by matching <c>ConfigurationItem.LineItemId</c> against
    /// <c>cart.Cart.Items[].Id</c> (see <c>ResolveFieldContextExtensions.GetConfiguratonItemCurrency</c>).
    /// On the mutation return path the back-reference is null in-memory, so no line item is found, the
    /// currency comes back null, and <c>ExtendedPrice.ToMoney(null)</c> throws ARGUMENT_NULL — whereas the
    /// cart/fullCart query path resolves it correctly because the persisted/re-read items carry LineItemId.
    ///
    /// The fix sets <c>LineItemId</c> on each configuration item so the in-memory mutation result is
    /// consistent with the query result and the currency resolver finds the owning line item.
    /// </summary>
    public class UpdateConfiguredLineItemConfigurationItemLineItemIdTests : XCartMoqHelper
    {
        [Fact]
        public async Task UpdateConfiguredLineItemAsync_SetsLineItemIdOnEachConfigurationItem()
        {
            // Arrange — a configured line item already in the cart.
            var cartAggregate = GetValidCartAggregate();

            var existingLineItem = new LineItem
            {
                Id = "line-1",
                ProductId = "configurable-1",
                IsConfigured = true,
                Currency = "USD",
                ConfigurationItems =
                [
                    new() { Id = "old-config-1", Type = "Product", SectionId = "section-A", ProductId = "old-product", LineItemId = "line-1" },
                ],
            };
            cartAggregate.Cart.Items = new List<LineItem> { existingLineItem };

            // The newly built configuration (as produced by CreateConfiguredLineItemCommand) does NOT
            // carry the LineItemId back-reference — exactly the state that reaches the resolver.
            var configuredItem = new LineItem
            {
                Currency = "USD",
                ConfigurationItems =
                [
                    new() { Id = "new-config-1", Type = "Product", SectionId = "section-A", ProductId = "new-product" },
                    new() { Id = "new-config-2", Type = "Text", SectionId = "section-T", CustomText = "hello" },
                ],
            };

            // Act
            await cartAggregate.UpdateConfiguredLineItemAsync(existingLineItem.Id, configuredItem);

            // Assert — every configuration item now carries the owning line item's id, so the
            // GetConfiguratonItemCurrency lookup (LineItemId == lineItem.Id) succeeds and the
            // extendedPrice money resolver no longer throws ARGUMENT_NULL.
            var updatedLineItem = cartAggregate.Cart.Items.Single(x => x.Id == "line-1");
            updatedLineItem.ConfigurationItems.Should().NotBeEmpty();
            updatedLineItem.ConfigurationItems.Should().OnlyContain(c => c.LineItemId == "line-1");
        }
    }
}
