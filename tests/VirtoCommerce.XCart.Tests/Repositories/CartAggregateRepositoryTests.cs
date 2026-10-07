using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Repositories
{
    public class CartAggregateRepositoryTests : XCartMoqHelper
    {
        private readonly Mock<IShoppingCartSearchService> _shoppingCartSearchService;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<ICurrencyService> _currencyService;
        private readonly Mock<IStoreService> _storeService;
        private readonly Mock<IMemberResolver> _memberResolver;
        private readonly PlatformMemoryCache _platformMemoryCache;

        private readonly CartAggregateRepository repository;

        public CartAggregateRepositoryTests()
        {
            _shoppingCartSearchService = new Mock<IShoppingCartSearchService>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _currencyService = new Mock<ICurrencyService>();
            _storeService = new Mock<IStoreService>();
            _memberResolver = new Mock<IMemberResolver>();

            _platformMemoryCache = new PlatformMemoryCache(
                new MemoryCache(Options.Create(new MemoryCacheOptions())),
                Options.Create(new CachingOptions()),
                new Mock<ILogger<PlatformMemoryCache>>().Object);

            repository = new CartAggregateRepository(
                () => _fixture.Create<CartAggregate>(),
                _shoppingCartSearchService.Object,
                _shoppingCartService.Object,
                _currencyService.Object,
                _memberResolver.Object,
                _storeService.Object,
                null,
                null,
                _fileUploadService.Object);
        }

        [Fact]
        public async Task RemoveCartAsync_ShouldCallShoppingCart()
        {
            // Arrange
            var cartId = _fixture.Create<string>();

            // Act
            await repository.RemoveCartAsync(cartId);

            // Assert
            _shoppingCartService.Verify(x => x.DeleteAsync(new List<string> { cartId }, It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// If this test fails check GetValidCartAggregate() from MoqHelper
        /// </summary>
        [Fact]
        public async Task SaveAsync_ShouldCallShoppingCart()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();

            // Act
            await repository.SaveAsync(cartAggregate);

            // Assert
            _shoppingCartService.Verify(x => x.SaveChangesAsync(new List<ShoppingCart> { cartAggregate.Cart }), Times.Once);
        }

        [Fact]
        public async Task GetCartAsync_ShoppingCartNotFound_ReturnNull()
        {
            // Arrange
            _shoppingCartSearchService
                .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ShoppingCartSearchResult
                {
                    Results = new List<ShoppingCart>()
                });

            var request = new ValidateCouponQuery
            {
                StoreId = It.IsAny<string>(),
                CartType = It.IsAny<string>(),
                CartName = It.IsAny<string>(),
                UserId = It.IsAny<string>(),
                OrganizationId = It.IsAny<string>(),
                CurrencyCode = It.IsAny<string>(),
                CultureName = It.IsAny<string>(),
            };

            // Act
            var result = await repository.GetCartAsync(request, It.IsAny<string>());

            // Assert
            result.Should().BeNull();
            _shoppingCartSearchService.Verify(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetCartByCriteriaAsync_ShoppingCartNotFound_ReturnNull()
        {
            // Arrange
            _shoppingCartSearchService
                .Setup(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ShoppingCartSearchResult
                {
                    Results = new List<ShoppingCart>()
                });

            var searchCartCriteria = new ShoppingCartSearchCriteria
            {
                Name = It.IsAny<string>(),
                StoreId = It.IsAny<string>(),
                CustomerId = It.IsAny<string>(),
                Currency = It.IsAny<string>(),
                Type = It.IsAny<string>(),
                ResponseGroup = It.IsAny<string>(),
            };

            // Act
            var result = await repository.GetCartAsync(searchCartCriteria, null);

            // Assert
            result.Should().BeNull();
            _shoppingCartSearchService.Verify(x => x.SearchAsync(It.IsAny<ShoppingCartSearchCriteria>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetCartForShoppingCartAsync_CartFound_AggregateReturnedCorrectly()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();

            var repository = new CartAggregateRepository(
                 () => cartAggregate,
                 _shoppingCartSearchService.Object,
                 _shoppingCartService.Object,
                 _currencyService.Object,
                 _memberResolver.Object,
                 _storeService.Object,
                 _cartProductServiceMock.Object,
                 _platformMemoryCache,
                 _fileUploadService.Object);

            var storeId = "Store";
            var store = _fixture.Create<Store>();
            store.Id = storeId;

            var shoppingCart = _fixture.Create<ShoppingCart>();
            shoppingCart.StoreId = storeId;

            _storeService.Setup(x => x.GetAsync(new[] { storeId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { store });

            var currencies = _fixture.CreateMany<Currency>(1).ToList();

            _currencyService.Setup(x => x.GetAllCurrenciesAsync())
                .ReturnsAsync(currencies);

            var customer = _fixture.Create<Contact>();
            _memberResolver.Setup(x => x.ResolveMemberByIdAsync(It.Is<string>(x => x == shoppingCart.CustomerId)))
                .ReturnsAsync(customer);

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync(new Dictionary<string, CartProduct>());

            // Act
            var result = await repository.GetCartForShoppingCartAsync(shoppingCart);

            // Assert
            result.Id.Should().Be(shoppingCart.Id);
            result.Cart.Should().Be(shoppingCart);
            result.Member.Should().Be(customer);
            result.Store.Should().Be(store);
            result.Currency.Code.Should().Be(currencies.FirstOrDefault().Code);
        }

        [Fact]
        public async Task GetCartByIdAsync_NarrowResponseGroup_DoesNotServeItToAFullRead()
        {
            // The aggregate is cached per response group, and the response group now decides whether the sharing
            // targets are loaded at all. Keying a narrowed load as "Full" handed a cart WITHOUT its recipients to
            // the next caller that asked for everything - silently, as an empty list rather than an error.
            var repository = new CartAggregateRepository(
                 () => GetValidCartAggregate(),
                 _shoppingCartSearchService.Object,
                 _shoppingCartService.Object,
                 _currencyService.Object,
                 _memberResolver.Object,
                 _storeService.Object,
                 _cartProductServiceMock.Object,
                 _platformMemoryCache,
                 _fileUploadService.Object);

            var storeId = "Store";
            var store = _fixture.Create<Store>();
            store.Id = storeId;
            _storeService.Setup(x => x.GetAsync(new[] { storeId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { store });

            var currencies = _fixture.CreateMany<Currency>(1).ToList();
            _currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync(currencies);

            var withoutTargets = _fixture.Create<ShoppingCart>();
            withoutTargets.StoreId = storeId;
            withoutTargets.SharingSettings = [new CartSharingSetting { Id = "key-1", Scope = "Customer", Targets = null }];

            var withTargets = _fixture.Create<ShoppingCart>();
            withTargets.Id = withoutTargets.Id;
            withTargets.CustomerId = withoutTargets.CustomerId;
            withTargets.StoreId = storeId;
            withTargets.SharingSettings =
            [
                new CartSharingSetting
                {
                    Id = "key-1",
                    Scope = "Customer",
                    Targets = [new CartSharingSettingTarget { SharedWithId = "org-1" }],
                },
            ];

            var customer = _fixture.Create<Contact>();
            _memberResolver.Setup(x => x.ResolveMemberByIdAsync(It.Is<string>(x => x == withoutTargets.CustomerId)))
                .ReturnsAsync(customer);

            var narrowResponseGroup = CartResponseGroup.WithLineItems.ToString();
            _shoppingCartService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), narrowResponseGroup, It.IsAny<bool>()))
                .ReturnsAsync(new List<ShoppingCart> { withoutTargets });
            _shoppingCartService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), null, It.IsAny<bool>()))
                .ReturnsAsync(new List<ShoppingCart> { withTargets });

            // Act: the narrow read first, so it is the one that populates the cache.
            await repository.GetCartByIdAsync(withoutTargets.Id, narrowResponseGroup, productsIncludeFields: null, cultureName: null);
            var result = await repository.GetCartByIdAsync(withoutTargets.Id, responseGroup: null, productsIncludeFields: null, cultureName: null);

            // Assert
            result.Cart.SharingSettings.Should().ContainSingle()
                .Which.Targets.Should().ContainSingle()
                .Which.SharedWithId.Should().Be("org-1");
        }

        [Fact]
        public async Task UpdateScopeAsync_PolicyMutatesThenThrows_LeavesNoMutationInTheCache()
        {
            // VCST-6113: the aggregate is cached by reference, so a scope write that mutated the cart before being
            // rejected stayed visible to every later read on this instance - and the next save persisted it, as a
            // scope the server itself refuses to create. A rejected write must leave the cache as it found it.
            // The policy here misbehaves on purpose: the guarantee is that a policy CANNOT poison the cache,
            // including one registered by another module through the public scope registry.
            var repository = CachingRepository();
            var storeId = "Store";
            var store = _fixture.Create<Store>();
            store.Id = storeId;
            store.DefaultLanguage = "en-US";
            store.Languages = ["en-US"];
            _storeService.Setup(x => x.GetAsync(new[] { storeId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { store });
            var currencies = _fixture.CreateMany<Currency>(1).ToList();
            _currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync(currencies);
            _memberResolver.Setup(x => x.ResolveMemberByIdAsync(It.IsAny<string>())).ReturnsAsync(_fixture.Create<Contact>());
            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync(new Dictionary<string, CartProduct>());

            var cartId = _fixture.Create<string>();
            var customerId = _fixture.Create<string>();

            // The first read populates the cache with the aggregate the handler is about to mutate.
            var aggregate = await repository.GetCartForShoppingCartAsync(SharedCart(cartId, customerId, storeId, currencies[0].Code));

            var sharingService = new Mock<ICartSharingService>();
            sharingService
                .Setup(x => x.UpdateScopeAsync(It.IsAny<ShoppingCart>(), It.IsAny<WishlistScopeContext>()))
                .Returns((ShoppingCart cart, WishlistScopeContext _) =>
                {
                    cart.SharingSettings[0].Scope = CartSharingScope.Organization;
                    cart.SharingSettings[0].Targets = [];

                    throw new InvalidOperationException("rejected after mutating");
                });

            var handler = new CacheProbeHandler(repository, sharingService.Object);

            // Act
            await handler.Invoking(x => x.UpdateScope(aggregate, ChangeCommand(cartId)))
                .Should().ThrowAsync<InvalidOperationException>();

            // A later request reloads the cart from storage; the cache must not answer with the rejected state.
            var reloaded = await repository.GetCartForShoppingCartAsync(SharedCart(cartId, customerId, storeId, currencies[0].Code));

            reloaded.Cart.SharingSettings.Should().ContainSingle()
                .Which.Scope.Should().Be(CartSharingScope.AnyoneAnonymous);
        }

        private CartAggregateRepository CachingRepository()
        {
            return new CartAggregateRepository(
                 () => GetValidCartAggregate(),
                 _shoppingCartSearchService.Object,
                 _shoppingCartService.Object,
                 _currencyService.Object,
                 _memberResolver.Object,
                 _storeService.Object,
                 _cartProductServiceMock.Object,
                 _platformMemoryCache,
                 _fileUploadService.Object);
        }

        private static ShoppingCart SharedCart(string cartId, string customerId, string storeId, string currencyCode)
        {
            return new ShoppingCart
            {
                Id = cartId,
                CustomerId = customerId,
                StoreId = storeId,
                Currency = currencyCode,
                SharingSettings =
                [
                    new CartSharingSetting
                    {
                        Id = "key-1",
                        Scope = CartSharingScope.AnyoneAnonymous,
                        Access = CartSharingAccess.Read,
                    },
                ],
            };
        }

        private static ChangeWishlistCommand ChangeCommand(string cartId)
        {
            return new ChangeWishlistCommand
            {
                ListId = cartId,
                Scope = CartSharingScope.Organization,
                WishlistUserContext = new WishlistUserContext
                {
                    CurrentUserId = "user-1",
                    CurrentContact = new Contact { Name = "Owner" },
                },
            };
        }

        // Exposes the protected seam the fix lives in; nothing else about the handler is under test here.
        private sealed class CacheProbeHandler(ICartAggregateRepository cartAggregateRepository, ICartSharingService cartSharingService)
            : ScopedWishlistCommandHandlerBase<ChangeWishlistCommand>(cartAggregateRepository, cartSharingService)
        {
            public Task UpdateScope(CartAggregate cartAggregate, ChangeWishlistCommand request) => UpdateScopeAsync(cartAggregate, request);

            public override Task<CartAggregate> Handle(ChangeWishlistCommand request, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        [Fact]
        public async Task GetCartForShoppingCartAsync_ProductPriceChanged_ShouldContainWarnings()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();

            var repository = new CartAggregateRepository(
                 () => cartAggregate,
                 _shoppingCartSearchService.Object,
                 _shoppingCartService.Object,
                 _currencyService.Object,
                 _memberResolver.Object,
                 _storeService.Object,
                 _cartProductServiceMock.Object,
                 _platformMemoryCache,
                 _fileUploadService.Object);

            var storeId = "Store";
            var store = _fixture.Create<Store>();
            store.Id = storeId;

            var shoppingCart = _fixture.Create<ShoppingCart>();
            var lineItem = _fixture.Create<LineItem>();
            shoppingCart.Items = new List<LineItem>() { lineItem };
            shoppingCart.StoreId = storeId;

            _storeService.Setup(x => x.GetAsync(new[] { storeId }, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { store });

            var currencies = _fixture.CreateMany<Currency>(1).ToList();

            _currencyService.Setup(x => x.GetAllCurrenciesAsync())
                .ReturnsAsync(currencies);

            var customer = _fixture.Create<Contact>();
            _memberResolver.Setup(x => x.ResolveMemberByIdAsync(It.Is<string>(x => x == shoppingCart.CustomerId)))
                .ReturnsAsync(customer);

            var product = _fixture.Create<CartProduct>();
            product.Id = lineItem.ProductId;

            lineItem.Currency = currencies.First().Code;

            product.ApplyPrices(
                    [
                        new Price
                        {
                            ProductId = product.Id,
                            PricelistId = _fixture.Create<string>(),
                            List = 1,
                            MinQuantity = 1,
                        }
                    ], currencies.First());

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string CurrencyCode, string ProductId)>>()))
                .ReturnsAsync(new Dictionary<string, CartProduct>()
                {
                    { CartAggregate.FormatGetCartProductKey(product.Id, "USD"), product },
                });

            // Act
            var result = await repository.GetCartForShoppingCartAsync(shoppingCart);

            // Assert
            result.ValidationWarnings.Should().HaveCount(1);
        }
    }
}
