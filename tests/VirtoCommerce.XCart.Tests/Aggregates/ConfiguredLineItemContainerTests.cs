using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
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
            _container.AddTextSectionLineItem(new ProductConfigurationSection { SectionId = "sec-text", SectionName = "Text Section", Type = ConfigurationSectionTypeText, CustomText = EngravingText });

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

            _container.AddFileSectionLineItem(new ProductConfigurationSection { SectionId = "sec-file", SectionName = "File Section", Type = ConfigurationSectionTypeFile }, files);

            _container.SourceAt(0).Should().BeNull();
            _container.FilesAt(0).Should().BeSameAs(files);
        }

        [Fact]
        public void CreateConfiguredLineItem_CreationPath_StampsSectionNameOnConfigurationItem()
        {
            _container.Store = new Store { Id = "store-1" };
            var cartProduct = NewCartProduct();

            _container.AddProductSectionLineItem(cartProduct, new ProductConfigurationSection { SectionId = "sec-product", SectionName = "Color", Type = ConfigurationSectionTypeProduct });

            var built = _container.CreateConfiguredLineItem(1).Item.ConfigurationItems.Single();
            built.SectionName.Should().Be("Color",
                "creation-path threads sectionName through staging, and CreateConfigurationItem stamps it onto the built ConfigurationItem");
        }

        [Fact]
        public void CreateConfiguredLineItem_SourceAware_PreservesSectionNameFromConfigurationItem()
        {
            _container.Store = new Store { Id = "store-1" };
            var configurationItem = NewConfigurationItem("sec-product", ConfigurationSectionTypeProduct);
            configurationItem.SectionName = "Color";
            var cartProduct = NewCartProduct();

            _container.AddProductSectionLineItem(cartProduct, configurationItem);

            var built = _container.CreateConfiguredLineItem(1).Item.ConfigurationItems.Single();
            built.SectionName.Should().Be("Color",
                "source-aware overload carries SectionName from the existing ConfigurationItem through staging onto the rebuilt item");
        }

        [Fact]
        public void AddProductSectionLineItem_CreationPath_StagesSectionAndCartProduct()
        {
            var configurationSection = new ProductConfigurationSection { SectionId = "sec-product", SectionName = "Color", Type = ConfigurationSectionTypeProduct };
            var cartProduct = NewCartProduct();

            _container.AddProductSectionLineItem(cartProduct, configurationSection);

            _container.SectionAt(0).Should().BeSameAs(configurationSection,
                "the creation-path configurationSection is staged so materialize-time builder dispatch can read it");
            _container.CartProductAt(0).Should().BeSameAs(cartProduct,
                "the configurationSection's chosen product is staged for the builder's subtype-specific field population");
        }

        [Fact]
        public void AddTextSectionLineItem_CreationPath_StagesSection_WithNullCartProduct()
        {
            var configurationSection = new ProductConfigurationSection { SectionId = "sec-text", SectionName = "Text Section", Type = ConfigurationSectionTypeText };

            _container.AddTextSectionLineItem(configurationSection);

            _container.SectionAt(0).Should().BeSameAs(configurationSection);
            _container.CartProductAt(0).Should().BeNull(
                "Text/File creation-path overloads have no chosen product — CartProduct is null by design");
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
            // Pricing is out of scope for these tests and requires a fully-priced ConfigurableProduct;
            // stub it so CreateConfiguredLineItem can run and we can assert on the built ConfigurationItems.
            public override void UpdatePrice(LineItem lineItem)
            {
            }

            public ConfigurationItem SourceAt(int index) => Items[index].Source;

            public string CustomTextAt(int index) => Items[index].CustomText;

            public IList<ConfigurationItemFile> FilesAt(int index) => Items[index].Files;

            public ProductConfigurationSection SectionAt(int index) => Items[index].ConfigurationSection;

            public CartProduct CartProductAt(int index) => Items[index].CartProduct;
        }
    }
}
