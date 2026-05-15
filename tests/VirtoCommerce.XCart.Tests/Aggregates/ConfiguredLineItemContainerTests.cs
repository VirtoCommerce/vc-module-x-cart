using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Aggregates
{
    /// <summary>
    /// Pins down the <c>SectionLineItem.Source</c> contract on
    /// <see cref="ConfiguredLineItemContainer"/>: source-aware overloads link
    /// <see cref="ConfigurationItem"/> by reference; creation-path overloads leave
    /// <c>Source</c> as <c>null</c>.
    /// </summary>
    public class ConfiguredLineItemContainerTests
    {
        private const string EngravingText = "ENGRAVING";

        private readonly TestableContainer _container;

        public ConfiguredLineItemContainerTests()
        {
            _container = new TestableContainer
            {
                Currency = new Currency(new Language("en-US"), "USD"),
                CultureName = "en-US",
                Store = new Store { Id = "test-store" },
            };
        }

        [Fact]
        public void AddProductSectionLineItem_WithConfigurationItem_LinksSourceByReference()
        {
            var configurationItem = NewConfigurationItem("sec-product", ConfigurationSectionTypeProduct);
            var cartProduct = NewCartProduct();

            _container.AddProductSectionLineItem(cartProduct, configurationItem);

            _container.SourceAt(0).Should().BeSameAs(configurationItem,
                "source-aware overloads must store the ConfigurationItem by reference, not a clone");
        }

        [Fact]
        public void AddTextSectionLineItem_WithConfigurationItem_LinksSource_AndCopiesCustomText()
        {
            var configurationItem = NewConfigurationItem("sec-text", ConfigurationSectionTypeText);
            configurationItem.CustomText = EngravingText;

            _container.AddTextSectionLineItem(configurationItem);

            _container.SourceAt(0).Should().BeSameAs(configurationItem);
            _container.CustomTextAt(0).Should().Be(EngravingText);
        }

        [Fact]
        public void AddFileSectionLineItem_WithConfigurationItem_NoOverride_UsesSourceFilesByReference()
        {
            var sourceFiles = new List<ConfigurationItemFile>
            {
                new() { Name = "front.png", Url = "/files/front.png" },
            };
            var configurationItem = NewConfigurationItem("sec-file", ConfigurationSectionTypeFile);
            configurationItem.Files = sourceFiles;

            _container.AddFileSectionLineItem(configurationItem);

            _container.SourceAt(0).Should().BeSameAs(configurationItem);
            _container.FilesAt(0).Should().BeSameAs(sourceFiles,
                "with files=null, the source file list must propagate as-is");
        }

        [Fact]
        public void AddFileSectionLineItem_WithExplicitFiles_OverridesFiles_AndPreservesSourceLink()
        {
            var configurationItem = NewConfigurationItem("sec-file", ConfigurationSectionTypeFile);
            configurationItem.Files = new List<ConfigurationItemFile> { new() { Name = "old.png" } };

            var customFiles = new List<ConfigurationItemFile>
            {
                new() { Name = "transformed.png", Url = "/files/transformed.png" },
            };

            _container.AddFileSectionLineItem(configurationItem, customFiles);

            _container.SourceAt(0).Should().BeSameAs(configurationItem,
                "explicit files override must not break the source link");
            _container.FilesAt(0).Should().BeSameAs(customFiles,
                "non-null files argument must override source.Files");
        }

        [Fact]
        public void AddTextSectionLineItem_CreationPath_LeavesSourceNull()
        {
            _container.AddTextSectionLineItem(EngravingText, "sec-text");

            _container.SourceAt(0).Should().BeNull(
                "creation-path overloads have no ConfigurationItem yet — Source is null by design");
            _container.CustomTextAt(0).Should().Be(EngravingText);
        }

        [Fact]
        public void AddFileSectionLineItem_CreationPath_LeavesSourceNull()
        {
            var files = new List<ConfigurationItemFile>
            {
                new() { Name = "creation.png" },
            };

            _container.AddFileSectionLineItem(files, "sec-file");

            _container.SourceAt(0).Should().BeNull();
            _container.FilesAt(0).Should().BeSameAs(files);
        }

        [Fact]
        public void AddProductSectionLineItem_CreationPath_FalseSelectedForCheckout_StoresOnInnerLineItem()
        {
            _container.ConfigurableProduct = NewCartProduct();

            _container.AddProductSectionLineItem(NewCartProduct(), quantity: 1, sectionId: "sec-1", type: ConfigurationSectionTypeProduct, selectedForCheckout: false);

            _container.InnerLineItemSelectedForCheckoutAt(0).Should().BeFalse(
                "creation-path overload must propagate selectedForCheckout to the synthetic LineItem so that CreateConfiguredLineItem can read it back");
        }

        [Fact]
        public void LineItemFactoryDefault_SelectedForCheckout_IsTrue_NoRegressionForLegacyOverload()
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();

            lineItem.SelectedForCheckout.Should().BeTrue();
        }

        [Fact]
        public void AddProductSectionLineItem_CreationPath_DefaultsToTrue()
        {
            _container.ConfigurableProduct = NewCartProduct();

            _container.AddProductSectionLineItem(NewCartProduct(), quantity: 1, sectionId: "sec-1", type: ConfigurationSectionTypeProduct);

            _container.InnerLineItemSelectedForCheckoutAt(0).Should().BeTrue(
                "default for the optional parameter must keep the existing behavior — item participates in checkout");
        }

        [Fact]
        public void CreateConfiguredLineItem_PropagatesSelectedForCheckoutFromInnerLineItem()
        {
            _container.ConfigurableProduct = NewCartProduct();

            _container.AddProductSectionLineItem(NewCartProduct(), quantity: 1, sectionId: "sec-1", type: ConfigurationSectionTypeProduct, selectedForCheckout: false);

            var result = _container.CreateConfiguredLineItem(quantity: 1);

            result.Item.ConfigurationItems.Should().HaveCount(1);
            result.Item.ConfigurationItems.Single().SelectedForCheckout.Should().BeFalse(
                "ConfigurationItem.SelectedForCheckout must reflect the value supplied in the mutation input");
        }

        [Fact]
        public void CreateConfiguredLineItem_DefaultsConfigurationItemToSelected_WhenInnerLineItemMissing()
        {
            _container.ConfigurableProduct = NewCartProduct();

            _container.AddTextSectionLineItem("ENGRAVING", "sec-text");

            var result = _container.CreateConfiguredLineItem(quantity: 1);

            result.Item.ConfigurationItems.Should().HaveCount(1);
            result.Item.ConfigurationItems.Single().SelectedForCheckout.Should().BeTrue(
                "sections without an inner LineItem (text/file) must default to selected, matching ConfigurableProductOption.SelectedForCheckout = true");
        }

        private static ConfigurationItem NewConfigurationItem(string sectionId, string type)
        {
            return new ConfigurationItem
            {
                SectionId = sectionId,
                Type = type,
            };
        }

        private static CartProduct NewCartProduct()
        {
            var catalogProduct = new CatalogProduct
            {
                Id = "test-product",
                Code = "TEST-CODE",
            };

            return new CartProduct(catalogProduct);
        }

        /// <summary>
        /// Exposes assertion-friendly accessors on the protected <c>Items</c> collection
        /// without leaking the protected <c>SectionLineItem</c> nested type into the outer
        /// test class.
        /// </summary>
        private sealed class TestableContainer : ConfiguredLineItemContainer
        {
            public ConfigurationItem SourceAt(int index) => Items[index].Source;

            public string CustomTextAt(int index) => Items[index].CustomText;

            public IList<ConfigurationItemFile> FilesAt(int index) => Items[index].Files;

            public bool? InnerLineItemSelectedForCheckoutAt(int index) => Items[index].Item?.SelectedForCheckout;

            // Pricing is out of scope for these tests and requires fully constructed
            // ProductPrice/Money graphs to avoid NREs from UpdatePrice. Bypass it.
            public override void UpdatePrice(LineItem lineItem)
            {
            }
        }
    }
}
