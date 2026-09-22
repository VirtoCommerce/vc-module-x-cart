using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Services;

public class SavedForLaterListServiceTests : XCartMoqHelper
{
    private const string ConfigurableProductId = "configurable-product";

    private readonly Mock<ICartAggregateRepository> _cartAggregateRepositoryMock = new();
    private readonly SavedForLaterListService _service;

    public SavedForLaterListServiceTests()
    {
        _service = new SavedForLaterListService(
            _cartAggregateRepositoryMock.Object,
            _cartProductServiceMock.Object,
            _fileUploadService.Object,
            _cartItemBuilder);

        SetupConfigurableProductLookup();
    }

    [Fact]
    public async Task MoveToSavedForLaterItems_ThreeConfigurationsOfSameProduct_KeptSeparate()
    {
        // Arrange
        var cart = BuildEmptyCart();
        cart.Cart.Items = new List<LineItem>
        {
            MakeTextConfiguredItem("li-1", "section-A", "variant-A"),
            MakeTextConfiguredItem("li-2", "section-B", "variant-B"),
            MakeTextConfiguredItem("li-3", "section-C", "variant-C"),
        };

        var savedForLater = BuildEmptyCart();
        SetupSavedForLaterFound(savedForLater);

        // Act
        var result = await _service.MoveToSavedForLaterItems(
            BuildRequest<MoveToSavedForLaterItemsCommand>(cart, "li-1", "li-2", "li-3"));

        // Assert
        result.Cart.Cart.Items.Should().BeEmpty();
        result.List.Cart.Items.Should().HaveCount(3);
        result.List.Cart.Items.Should().OnlyContain(x => x.IsConfigured && x.ProductId == ConfigurableProductId);
        result.List.Cart.Items
            .SelectMany(x => x.ConfigurationItems)
            .Select(c => c.CustomText)
            .Should().BeEquivalentTo(["variant-A", "variant-B", "variant-C"]);
    }

    [Fact]
    public async Task MoveFromSavedForLaterItems_ConfiguredItemWithUploadedFile_RestoresConfigurationAndFile()
    {
        // Arrange
        const string fileUrl = "/api/files/uploaded-spec.pdf";
        var savedForLater = BuildEmptyCart();
        savedForLater.Cart.Items = [MakeFileConfiguredItem("li-1", "section-files", fileUrl)];

        var cart = BuildEmptyCart();

        SetupSavedForLaterFound(savedForLater);
        SetupFileLookup(fileUrl, owner: savedForLater.Cart);

        // Act
        var result = await _service.MoveFromSavedForLaterItems(
            BuildRequest<MoveFromSavedForLaterItemsCommand>(cart, "li-1"));

        // Assert
        result.List.Cart.Items.Should().BeEmpty();

        var moved = result.Cart.Cart.Items.Should().ContainSingle().Subject;
        moved.IsConfigured.Should().BeTrue();
        moved.ProductId.Should().Be(ConfigurableProductId);
        moved.ConfigurationItems.Should().ContainSingle()
            .Which.Files.Should().ContainSingle()
            .Which.Url.Should().Be(fileUrl);
    }

    [Fact]
    public async Task MoveToSavedForLaterItems_ConfiguredItem_PreservesNote()
    {
        // Arrange
        var sourceItem = MakeTextConfiguredItem("li-1", "section-X", "the-text");
        sourceItem.Note = "do not forget";

        var cart = BuildEmptyCart();
        cart.Cart.Items = [sourceItem];

        var savedForLater = BuildEmptyCart();
        SetupSavedForLaterFound(savedForLater);

        // Act
        var result = await _service.MoveToSavedForLaterItems(
            BuildRequest<MoveToSavedForLaterItemsCommand>(cart, "li-1"));

        // Assert
        var moved = result.List.Cart.Items.Should().ContainSingle().Subject;
        moved.Note.Should().Be("do not forget");
        moved.ConfigurationItems.Should().ContainSingle()
            .Which.CustomText.Should().Be("the-text");
    }

    [Fact]
    public async Task MoveToSavedForLaterItems_UnknownLineItemId_DoesNothing()
    {
        // Arrange
        var cart = BuildEmptyCart();
        cart.Cart.Items = [MakeTextConfiguredItem("li-real", "section", "text")];

        var savedForLater = BuildEmptyCart();
        SetupSavedForLaterFound(savedForLater);

        // Act
        var result = await _service.MoveToSavedForLaterItems(
            BuildRequest<MoveToSavedForLaterItemsCommand>(cart, "li-does-not-exist"));

        // Assert
        result.Cart.Cart.Items.Should().HaveCount(1);
        result.List.Cart.Items.Should().BeEmpty();
        _cartAggregateRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CartAggregate>()), Times.Never);
    }

    [Fact]
    public async Task MoveFromSavedForLaterItems_FileOwnedByDifferentCart_NotCarriedOver()
    {
        // Arrange
        const string fileUrl = "/api/files/spec.pdf";
        var savedForLater = BuildEmptyCart();
        savedForLater.Cart.Items = [MakeFileConfiguredItem("li-1", "section-files", fileUrl)];

        var cart = BuildEmptyCart();
        var unrelatedCart = BuildEmptyCart();

        SetupSavedForLaterFound(savedForLater);
        SetupFileLookup(fileUrl, owner: unrelatedCart.Cart);

        // Act
        var result = await _service.MoveFromSavedForLaterItems(
            BuildRequest<MoveFromSavedForLaterItemsCommand>(cart, "li-1"));

        // Assert
        var moved = result.Cart.Cart.Items.Should().ContainSingle().Subject;
        moved.ConfigurationItems.Should().ContainSingle()
            .Which.Files.Should().BeNullOrEmpty();
    }

    private CartAggregate BuildEmptyCart()
    {
        var aggregate = GetValidCartAggregate();

        aggregate.Cart.Items = [];

        return aggregate;
    }

    private static LineItem MakeTextConfiguredItem(string id, string sectionId, string customText)
    {
        return new LineItem
        {
            Id = id,
            ProductId = ConfigurableProductId,
            IsConfigured = true,
            Quantity = 1,
            ConfigurationItems =
            [
                new()
                {
                    SectionId = sectionId,
                    Type = ConfigurationSectionTypeText,
                    CustomText = customText,
                },
            ],
        };
    }

    private static LineItem MakeFileConfiguredItem(string id, string sectionId, string fileUrl)
    {
        return new LineItem
        {
            Id = id,
            ProductId = ConfigurableProductId,
            IsConfigured = true,
            Quantity = 1,
            ConfigurationItems =
            [
                new()
                {
                    SectionId = sectionId,
                    Type = ConfigurationSectionTypeFile,
                    Files = [new() { Url = fileUrl }],
                },
            ],
        };
    }

    private void SetupConfigurableProductLookup()
    {
        _cartProductServiceMock
            .Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<string>>()))
            .ReturnsAsync((CartAggregate _, IList<string> ids) => ids
                .Select(id => new CartProduct(new CatalogProduct { Id = id, Name = id }))
                .ToList());
    }

    private void SetupSavedForLaterFound(CartAggregate savedForLater)
    {
        _cartAggregateRepositoryMock
            .Setup(x => x.GetCartAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(savedForLater);
    }

    // GetByPublicUrlAsync is an extension method (Moq can't intercept it directly);
    // it parses an id from each URL and delegates to IFileUploadService.GetAsync.
    // OwnerEntityType must be the FullName, not nameof — that's what OwnerIs(cart) compares against.
    private void SetupFileLookup(string fileUrl, ShoppingCart owner)
    {
        var file = new File
        {
            Id = fileUrl.Split('/').Last(),
            PublicUrl = fileUrl,
            Name = "spec.pdf",
            Scope = ConfigurationSectionFilesScope,
            OwnerEntityId = owner.Id,
            OwnerEntityType = typeof(ShoppingCart).FullName,
        };

        _fileUploadService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([file]);
    }

    private static TCommand BuildRequest<TCommand>(CartAggregate userCart, params string[] lineItemIds)
        where TCommand : MoveSavedForLaterItemsCommandBase, new()
    {
        return new TCommand
        {
            Cart = userCart,
            LineItemIds = lineItemIds,
            StoreId = DEFAULT_STORE_ID,
            UserId = "test-user",
            CurrencyCode = CURRENCY_CODE,
            CultureName = CULTURE_NAME,
        };
    }
}
