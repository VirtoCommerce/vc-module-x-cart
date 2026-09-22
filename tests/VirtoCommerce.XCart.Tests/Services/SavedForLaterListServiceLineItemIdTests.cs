using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Services;

/// <summary>
/// End-to-end regression coverage for the broadened VCST-5391 fix. Every configuration-building path
/// (add-to-cart, move-from-saved-for-later, edit) persists the destination cart through the central
/// <see cref="CartAggregateRepository.SaveAsync"/>. On a freshly built configured line item the id is
/// <c>null</c>; <c>IShoppingCartService.SaveChangesAsync</c> assigns the real line item id during persistence
/// but does NOT back-fill each configuration item's <c>LineItemId</c>. The central stamp in
/// <c>SaveAsync</c> closes that gap so the mutation response returned to the GraphQL resolvers carries a
/// consistent back-reference (<c>ConfigurationItem.LineItemId == lineItem.Id</c>) — the key the
/// extendedPrice currency resolver matches on.
///
/// This test drives the REAL <see cref="CartAggregateRepository"/> with a mocked <see cref="IShoppingCartService"/>
/// whose <c>SaveChangesAsync</c> ONLY assigns line item ids (mirroring the CartModule NuGet contract) and never
/// touches <c>ConfigurationItem.LineItemId</c>. Therefore the asserted back-reference can only come from the
/// production stamp under test — neutralizing that stamp makes this test fail.
/// </summary>
public class SavedForLaterListServiceLineItemIdTests : XCartMoqHelper
{
    private const string ConfigurableProductId = "configurable-product";

    private readonly Mock<IShoppingCartSearchService> _shoppingCartSearchServiceMock = new();
    private readonly Mock<IShoppingCartService> _shoppingCartServiceMock = new();
    private readonly Mock<IStoreService> _storeServiceMock = new();
    private readonly Mock<IMemberResolver> _memberResolverMock = new();
    private readonly PlatformMemoryCache _platformMemoryCache;
    private readonly CartAggregateRepository _repository;

    public SavedForLaterListServiceLineItemIdTests()
    {
        _platformMemoryCache = new PlatformMemoryCache(
            new MemoryCache(Options.Create(new MemoryCacheOptions())),
            Options.Create(new CachingOptions()),
            new Mock<ILogger<PlatformMemoryCache>>().Object);

        // Mirror the CartModule NuGet contract: persistence assigns ids to newly created line items but does
        // NOT back-fill ConfigurationItem.LineItemId. The stamp must come from production, not this mock.
        _shoppingCartServiceMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<ShoppingCart>>()))
            .Callback((IList<ShoppingCart> carts) =>
            {
                foreach (var item in carts.SelectMany(c => c.Items).Where(x => string.IsNullOrEmpty(x.Id)))
                {
                    item.Id = $"persisted-{item.ProductId}";
                }
            })
            .Returns(Task.CompletedTask);

        _repository = new CartAggregateRepository(
            () => _fixture.Create<CartAggregate>(),
            _shoppingCartSearchServiceMock.Object,
            _shoppingCartServiceMock.Object,
            _currencyServiceMock.Object,
            _memberResolverMock.Object,
            _storeServiceMock.Object,
            _cartProductServiceMock.Object,
            _platformMemoryCache,
            _fileUploadService.Object);
    }

    [Fact]
    public async Task SaveAsync_NewlyAddedConfiguredItem_StampsConfigurationItemLineItemId()
    {
        // Arrange — a configured line item freshly built by a mutation (move/add): no persisted id yet.
        var aggregate = GetValidCartAggregate();
        aggregate.Cart.Items = [MakeTextConfiguredItem(id: null, "section-A", "engraving")];

        // Act — the real central save: SaveChangesAsync assigns the id, then production stamps the back-reference.
        await _repository.SaveAsync(aggregate);

        // Assert — the moved/added configured item carries the back-reference the extendedPrice resolver needs.
        var saved = aggregate.Cart.Items.Should().ContainSingle().Subject;
        saved.IsConfigured.Should().BeTrue();
        saved.Id.Should().NotBeNullOrEmpty();
        saved.ConfigurationItems.Should().NotBeEmpty();
        saved.ConfigurationItems.Should().OnlyContain(c => c.LineItemId == saved.Id);
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
}
