using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Services
{
    /// <summary>
    /// Covers <see cref="ICartItemBuilder"/> contract end-to-end: default behaviour,
    /// <see cref="AbstractTypeFactory{T}"/> override dispatch, container property fallback,
    /// and mapper-context fallback. Each test that registers a factory override removes it
    /// in a <c>finally</c> block — the factory is global static and must be isolated between tests.
    /// </summary>
    public class CartItemBuilderTests
    {
        // === Direct builder — LineItem ===

        [Fact]
        public void Create_LineItem_NoOverride_ReturnsBase()
        {
            // Empty factory == production state when no module registers a LineItem override.
            var builder = new CartItemBuilder();

            var result = builder.Create(BuildCartProduct());

            result.Should().BeOfType<LineItem>();
        }

        [Fact]
        public void Create_LineItem_WithOverride_ReturnsSubtype()
        {
            AbstractTypeFactory<LineItem>.OverrideType<LineItem, TestLineItem>();
            try
            {
                var builder = new CartItemBuilder();

                var result = builder.Create(BuildCartProduct());

                result.Should().BeOfType<TestLineItem>();
            }
            finally
            {
                AbstractTypeFactory<LineItem>.RemoveType<TestLineItem>();
            }
        }

        // === Direct builder — ConfigurationItem ===

        [Fact]
        public void Create_ConfigurationItem_NoOverride_ReturnsBase()
        {
            var builder = new CartItemBuilder();

            var result = builder.Create(new ProductConfigurationSection());

            result.Should().BeOfType<ConfigurationItem>();
        }

        [Fact]
        public void Create_ConfigurationItem_WithOverride_ReturnsSubtype()
        {
            AbstractTypeFactory<ConfigurationItem>.OverrideType<ConfigurationItem, TestConfigurationItem>();
            try
            {
                var builder = new CartItemBuilder();

                var result = builder.Create(new ProductConfigurationSection());

                result.Should().BeOfType<TestConfigurationItem>();
            }
            finally
            {
                AbstractTypeFactory<ConfigurationItem>.RemoveType<TestConfigurationItem>();
            }
        }

        // === Fallback discipline ===

        [Fact]
        public void Container_WithoutCartItemBuilder_FallsBackToTryCreateInstance()
        {
            // Direct-instantiation sites (SavedForLaterListService, ConfiguredLineItemContainerService,
            // ChangeCartCurrencyCommandHandler) don't go through CartAggregate.CreateConfiguredLineItemContainer
            // and therefore don't populate CartItemBuilder. The container must remain functional via the
            // null-conditional fallback to TryCreateInstance() — no behaviour regression for these callers.
            var container = new ConfiguredLineItemContainer { CultureName = "en-US" };

            var lineItem = container.CreateLineItem(BuildCartProduct(), quantity: 1);

            lineItem.Should().BeOfType<LineItem>();
            lineItem.ProductId.Should().Be("p1");
        }

        [Fact]
        public void XCartMapper_ToLineItem_WithoutBuilder_FallsBackToTryCreateInstance()
        {
            var mapper = new XCartMapper();

            var lineItem = mapper.ToLineItem(BuildCartProduct(), new CartMappingContext { CultureName = "en-US" });

            lineItem.Should().BeOfType<LineItem>();
            lineItem.ProductId.Should().Be("p1");
        }

        private static CartProduct BuildCartProduct() => new(new CatalogProduct
        {
            Id = "p1",
            Code = "SKU001",
            CatalogId = "cat",
            CategoryId = "cat-1",
        });

        private sealed class TestLineItem : LineItem;

        private sealed class TestConfigurationItem : ConfigurationItem;
    }
}
