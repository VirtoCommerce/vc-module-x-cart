using System.Collections.Generic;
using FluentAssertions;
using VirtoCommerce.XCart.Data.Extensions;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Extensions;

public class FieldsExtensionsTests
{
    [Fact]
    public void ItemsToProductIncludeField_NullInput_ReturnsNull()
    {
        IList<string> includeFields = null;

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeNull();
    }

    [Fact]
    public void ItemsToProductIncludeField_EmptyInput_ReturnsEmpty()
    {
        var includeFields = new List<string>();

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ItemsToProductIncludeField_LineItemProductPath_IsExtracted()
    {
        var includeFields = new List<string> { "items.product.id", "items.product.name" };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "name"]);
    }

    [Fact]
    public void ItemsToProductIncludeField_NestedLineItemProductPath_IsExtracted()
    {
        var includeFields = new List<string> { "items.items.product.id", "items.items.product.properties.tier" };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "properties.tier"]);
    }

    [Fact]
    public void ItemsToProductIncludeField_ConfigurationItemProductPath_IsExtracted()
    {
        var includeFields = new List<string>
        {
            "items.configurationItems.product.id",
            "items.configurationItems.product.properties.chargeCode",
        };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "properties.chargeCode"]);
    }

    [Fact]
    public void ItemsToProductIncludeField_NestedConfigurationItemProductPath_IsExtracted()
    {
        var includeFields = new List<string>
        {
            "items.items.configurationItems.product.id",
            "items.items.configurationItems.product.properties.colorCode",
        };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "properties.colorCode"]);
    }

    [Fact]
    public void ItemsToProductIncludeField_MixedLineItemAndCi_BothIncluded()
    {
        var includeFields = new List<string>
        {
            "items.product.id",
            "items.product.name",
            "items.configurationItems.product.id",
            "items.configurationItems.product.properties.chargeCode",
        };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "name", "id", "properties.chargeCode"]);
    }

    [Fact]
    public void ItemsToProductIncludeField_NonProductPaths_AreExcluded()
    {
        var includeFields = new List<string>
        {
            "items.id",
            "items.name",
            "items.configurationItems.id",
            "items.configurationItems.text",
            "totals.total",
        };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ItemsToProductIncludeField_MixedRelevantAndIrrelevant_OnlyProductFieldsKept()
    {
        var includeFields = new List<string>
        {
            "items.id",
            "items.product.id",
            "items.configurationItems.id",
            "items.configurationItems.product.properties.chargeCode",
            "totals.total",
        };

        var result = includeFields.ItemsToProductIncludeField();

        result.Should().BeEquivalentTo(["id", "properties.chargeCode"]);
    }
}
