using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Validators;

public class CartValidatorTests : XCartMoqHelper
{
    private readonly CartValidator _validator = new CartValidator();

    [Fact]
    public async Task ValidateCart_EmptyCart_Valid()
    {
        // Arrange
        var aggregate = GetValidCartAggregate();
        aggregate.Cart.Items.Clear();
        aggregate.Cart.Shipments.Clear();
        aggregate.Cart.Payments.Clear();

        // Act
        var result = await _validator.ValidateAsync(new CartValidationContext
        {
            CartAggregate = aggregate,
        }, options => options.IncludeRuleSets("default", "items", "shipments", "payments"), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateCart_RuleSetStrict_Invalid()
    {
        // Arrange
        var aggregate = GetValidCartAggregate();
        aggregate.Cart.Name = null;
        aggregate.Cart.CustomerId = null;

        // Act
        var result = await _validator.ValidateAsync(new CartValidationContext
        {
            CartAggregate = aggregate,
        }, options => options.IncludeRuleSets("default", "items", "shipments", "payments"), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(8);
    }

    [Fact]
    public async Task ValidateCart_RuleSetDefault_Valid()
    {
        // Arrange
        var aggregate = GetValidCartAggregate();

        // Act
        var result = await _validator.ValidateAsync(new CartValidationContext
        {
            CartAggregate = aggregate,
        }, options => options.IncludeRuleSets("default"), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateCart_ApplyRuleOverride()
    {
        // Arrange
        var aggregate = GetValidCartAggregate();

        var validator2 = new CartValidator2();

        // Act
        var result = await validator2.ValidateAsync(new CartValidationContext
        {
            CartAggregate = aggregate,
        }, options => options.IncludeRuleSets("items"), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Should().Contain(x => x.ErrorMessage == "FakeFailure");
    }

    [Fact]
    public async Task ValidateOrderCreate_ConditionalSection_ParentNotSelected_Valid()
    {
        // Arrange: Section B is required but depends on Section A. Section A is not selected — Section B must not be enforced.
        const string productId = "productA";
        const string sectionAId = "sectionA";
        const string sectionBId = "sectionB";

        var lineItem = new LineItem
        {
            ProductId = productId,
            IsGift = false,
            SelectedForCheckout = true,
            IsConfigured = true,
            ConfigurationItems = [],
        };

        var cart = GetCart();
        cart.Items = [lineItem];

        var configuration = new ProductConfiguration
        {
            Sections =
            [
                new ProductConfigurationSection { Id = sectionAId, IsRequired = false },
                new ProductConfigurationSection { Id = sectionBId, IsRequired = true, DependsOnSectionId = sectionAId },
            ],
        };

        var context = new CartValidationContext
        {
            CartAggregate = GetValidCartAggregate(cart),
            ProductConfigurations = new Dictionary<string, ProductConfiguration> { { productId, configuration } },
        };

        // Act
        var result = await _validator.ValidateAsync(context, options => options.IncludeRuleSets("orderCreate"), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOrderCreate_ConditionalSection_ParentSelected_RequiredChildMissing_Invalid()
    {
        // Arrange: Section B is required and depends on Section A. Section A IS selected, so Section B becomes required.
        const string productId = "productA";
        const string sectionAId = "sectionA";
        const string sectionBId = "sectionB";

        var lineItem = new LineItem
        {
            ProductId = productId,
            IsGift = false,
            SelectedForCheckout = true,
            IsConfigured = true,
            ConfigurationItems =
            [
                new ConfigurationItem { SectionId = sectionAId, SelectedForCheckout = true },
            ],
        };

        var cart = GetCart();
        cart.Items = [lineItem];

        var configuration = new ProductConfiguration
        {
            Sections =
            [
                new ProductConfigurationSection { Id = sectionAId, IsRequired = false },
                new ProductConfigurationSection { Id = sectionBId, IsRequired = true, DependsOnSectionId = sectionAId },
            ],
        };

        var context = new CartValidationContext
        {
            CartAggregate = GetValidCartAggregate(cart),
            ProductConfigurations = new Dictionary<string, ProductConfiguration> { { productId, configuration } },
        };

        // Act
        var result = await _validator.ValidateAsync(context, options => options.IncludeRuleSets("orderCreate"), TestContext.Current.CancellationToken);

        // Assert: Section B is now required because its parent (Section A) was selected
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.ErrorCode == "CONFIGURATION_SECTION_REQUIRED");
    }
}
