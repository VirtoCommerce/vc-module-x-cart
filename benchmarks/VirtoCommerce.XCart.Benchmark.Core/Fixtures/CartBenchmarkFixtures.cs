using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Mapping;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared fixture builders for the XCart benchmarks. The single design rule: everything that does
/// I/O is a mock, everything that is pure compute runs for real. In particular the totals calculator
/// is the real <see cref="DefaultShoppingCartTotalsCalculator"/> — mocking it measures an
/// almost-empty <c>RecalculateAsync</c>.
/// </summary>
public static class CartBenchmarkFixtures
{
    public const string StoreId = "benchmark-store";

    public static readonly Currency Currency = new(new Language("en-US"), "USD")
    {
        ExchangeRate = 1m,
        RoundingPolicy = new DefaultMoneyRoundingPolicy(),
    };

    public static Store CreateStore() => new() { Id = StoreId, Settings = [] };

    /// <summary>
    /// Builds a shopping cart with <paramref name="lineItemCount"/> selected line items of the
    /// given <paramref name="shape"/>. OrganizationId is left empty so RecalculateAsync's
    /// UpdateOrganizationName short-circuits without an I/O call. Name/CustomerId/LanguageCode are
    /// populated so the cart is also valid for the mutate-existing-cart load path (which falls back
    /// to store.DefaultLanguage when LanguageCode is empty) and for ruleset-gated validators.
    /// </summary>
    public static ShoppingCart CreateCart(int lineItemCount, CartShape shape)
    {
        var items = new List<LineItem>(lineItemCount);

        for (var i = 0; i < lineItemCount; i++)
        {
            var item = AbstractTypeFactory<LineItem>.TryCreateInstance();
            item.Id = $"li-{i}";
            item.ProductId = $"product-{i}";
            item.CatalogId = "catalog";
            item.Sku = $"SKU-{i}";
            item.Name = $"Product {i}";
            item.Currency = Currency.Code;
            item.Quantity = 2;
            item.ListPrice = 10m;
            item.SalePrice = 9m;
            item.SelectedForCheckout = true;
            item.DynamicProperties = []; // never null on a real loaded item — changeCartCurrency's CopyItems does DynamicProperties.SelectMany unguarded

            if (shape == CartShape.Configured)
            {
                item.IsConfigured = true;
                item.ConfigurationItems = CreateConfigurationItems(i);
            }

            items.Add(item);
        }

        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();
        cart.Id = "benchmark-cart";
        cart.Name = "default";
        cart.StoreId = StoreId;
        cart.CustomerId = "benchmark-user";
        cart.Currency = Currency.Code;
        cart.LanguageCode = "en-US";
        cart.Items = items;
        cart.Shipments = [];
        cart.Payments = [];
        cart.Coupons = []; // a real cart's Coupons collection is never null — coupon mutations call .Any()/.Remove() on it
        cart.Addresses = []; // likewise: ClearAsync/address handlers call .Clear()/LINQ on Addresses

        return cart;
    }

    /// <summary>Three priced variation items per configured line item — the configured-shape graph
    /// reused by every cluster's fixtures.</summary>
    public static List<ConfigurationItem> CreateConfigurationItems(int lineItemIndex)
    {
        // Enough object graph to make the configured shape diverge from flat without modelling a
        // full design→garment tree.
        return Enumerable.Range(0, 3).Select(v =>
        {
            var item = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();
            item.Id = $"ci-{lineItemIndex}-{v}";
            item.Type = "Variation";
            item.ProductId = $"variation-{lineItemIndex}-{v}";
            item.Quantity = 1;
            return item;
        }).ToList();
    }

    // ── addCartItems command-level harness ──────────────────────────────────────────────────────

    /// <summary>Real AutoMapper from the production cart profile — the add path maps CartProduct → LineItem.</summary>
    public static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<CartMappingProfile>()).CreateMapper();

    /// <summary>An active/buyable, untracked, priced <see cref="CartProduct"/> — the success-path
    /// product shape reused across clusters.</summary>
    public static CartProduct CreateCartProduct(string productId)
    {
        // Active + buyable + no inventory tracking so the Strict add-validation rules pass and the
        // item is actually added (an invalid product makes AddItemAsync return early — measuring
        // nothing). A real ProductPrice drives SetLineItemTierPrice on add.
        var product = AbstractTypeFactory<CatalogProduct>.TryCreateInstance();
        product.Id = productId;
        product.CatalogId = "catalog";
        product.Code = $"SKU-{productId}";
        product.Name = $"Product {productId}";
        product.IsActive = true;
        product.IsBuyable = true;
        product.TrackInventory = false;

        return new CartProduct(product)
        {
            Price = new ProductPrice(Currency)
            {
                ListPrice = new Money(10m, Currency),
                SalePrice = new Money(9m, Currency),
            },
        };
    }

    /// <summary>
    /// An <c>addCartItems</c> command of <paramref name="itemCount"/> items with no CartId
    /// (create-new path). The count is the bulk dimension: 1 = single add, &gt;1 = bulk.
    /// </summary>
    public static AddCartItemsCommand CreateAddCartItemsCommand(int itemCount)
    {
        // Built via the factory (not new) so a consumer's OverrideCommandType<AddCartItemsCommand, …>
        // yields the consumer's command subtype — MediatR dispatches on the runtime type, so this is
        // what routes a Send to the overridden handler.
        var command = AbstractTypeFactory<AddCartItemsCommand>.TryCreateInstance();
        command.StoreId = StoreId;
        command.CurrencyCode = Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.CartItems = Enumerable.Range(0, itemCount)
            .Select(i => new NewCartItem($"product-{i}", quantity: 2))
            .ToArray();

        return command;
    }

    // ── mutate-existing-cart harness ─────────────────────────────────────────────────────────────
    // Mutation handlers (change-quantity, change-price, remove-item, configuration, ...) reach the
    // cart through CartCommandHandler.GetOrCreateCartFromCommandAsync → CartId set →
    // CartAggregateRepository.GetCartByIdAsync → IShoppingCartService.GetByIdAsync → the CACHED
    // InnerGetCartAggregateFromCartAsync branch (cart.Id is non-empty). Two harness pieces make this
    // benchmarkable and idempotent: a never-cache IPlatformMemoryCache (every call is a miss, so the
    // real load+recalc runs each time) and a GetAsync mock that returns a FRESH populated cart per
    // call (so a mutation never accumulates across invocations). No [IterationSetup] needed → Mean
    // precision is preserved (InvocationCount is not forced to 1).

    /// <summary>
    /// A never-cache <see cref="IPlatformMemoryCache"/>: <c>TryGetValue</c> always misses (so
    /// <c>GetOrCreateExclusiveAsync</c> runs the factory — the real load+recalc — every call), and
    /// <c>CreateEntry</c> returns an entry whose <c>ExpirationTokens</c>/<c>PostEvictionCallbacks</c>
    /// are real (empty) lists so the trailing <c>cache.Set</c> → <c>SetOptions</c> copy loop (the
    /// factory adds two expiration tokens) doesn't NRE on a null collection. Fresh lists per access
    /// keep that mock plumbing out of the measured allocation delta.
    /// </summary>
    public static Mock<IPlatformMemoryCache> NeverCacheMock()
    {
        var cache = new Mock<IPlatformMemoryCache>();
        object cached = null;
        cache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cached)).Returns(false);
        cache.Setup(x => x.GetDefaultCacheEntryOptions()).Returns(() => new MemoryCacheEntryOptions());
        cache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(() =>
        {
            var entry = new Mock<ICacheEntry>();
            entry.SetupGet(e => e.ExpirationTokens).Returns(new List<IChangeToken>());
            entry.SetupGet(e => e.PostEvictionCallbacks).Returns(new List<PostEvictionCallbackRegistration>());
            return entry.Object;
        });
        return cache;
    }

    /// <summary>The shared product mock: returns one active/buyable priced <see cref="CartProduct"/>
    /// per requested (currency, product) pair (<c>GetCartProductsAsync</c>) or per product id
    /// (<c>GetCartProductsByIdsAsync</c> — used by the configured saved-for-later copy path).</summary>
    public static Mock<ICartProductService> CartProductServiceMock()
    {
        var mock = new Mock<ICartProductService>();
        mock.Setup(x => x.GetCartProductsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<(string, string)>>()))
            .ReturnsAsync((CartAggregate aggregate, IList<(string CurrencyCode, string ProductId)> pairs) =>
                pairs.ToDictionary(
                    p => aggregate.GetCartProductKey(p.ProductId, p.CurrencyCode),
                    p => CreateCartProduct(p.ProductId)));
        mock.Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), It.IsAny<IList<string>>()))
            .ReturnsAsync((CartAggregate _, IList<string> ids) =>
                ids.Select(CreateCartProduct).ToList());
        return mock;
    }

    /// <summary>Stamps the shared cart context (target cart id + store/currency/culture/user) onto
    /// any <see cref="CartCommand"/> so every mutation command resolves the same loaded cart.</summary>
    public static T WithCartContext<T>(T command)
        where T : CartCommand
    {
        command.CartId = "benchmark-cart";
        command.StoreId = StoreId;
        command.CurrencyCode = Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";

        return command;
    }

    /// <summary>A <c>changeCartItemQuantity</c> command targeting the first line item of the loaded
    /// cart (<c>li-0</c>), set to a new non-zero quantity (so it takes the change-quantity path, not
    /// the remove-on-zero path).</summary>
    public static ChangeCartItemQuantityCommand CreateChangeCartItemQuantityCommand()
    {
        var command = AbstractTypeFactory<ChangeCartItemQuantityCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.Quantity = 5;

        return WithCartContext(command);
    }

    /// <summary>A <c>changeCartItemPrice</c> command setting a manual price on the first line item.
    /// The Strict ruleset rejects a price below the line item's current SalePrice, and that loaded
    /// value differs by shape — flat is 9, but a configured item's price is the sum of its variation
    /// sections (~36 for the 3-variation fixture). The manual price must clear the larger (configured)
    /// value so the success path is measured for both shapes; 100 gives headroom.</summary>
    public static ChangeCartItemPriceCommand CreateChangeCartItemPriceCommand()
    {
        var command = AbstractTypeFactory<ChangeCartItemPriceCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.Price = 100m;

        return WithCartContext(command);
    }

    /// <summary>A <c>changeCartItemComment</c> command setting a comment on the first line item.</summary>
    public static ChangeCartItemCommentCommand CreateChangeCartItemCommentCommand()
    {
        var command = AbstractTypeFactory<ChangeCartItemCommentCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.Comment = "benchmark comment";

        return WithCartContext(command);
    }

    /// <summary>A <c>changeCartItemSelected</c> command toggling the first line item's checkout selection off.</summary>
    public static ChangeCartItemSelectedCommand CreateChangeCartItemSelectedCommand()
    {
        var command = AbstractTypeFactory<ChangeCartItemSelectedCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.SelectedForCheckout = false;

        return WithCartContext(command);
    }

    /// <summary>A <c>removeCartItem</c> command removing the first line item of the loaded cart.</summary>
    public static RemoveCartItemCommand CreateRemoveCartItemCommand()
    {
        var command = AbstractTypeFactory<RemoveCartItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";

        return WithCartContext(command);
    }

    /// <summary>A <c>changeAllCartItemsSelected</c> command toggling EVERY line item's checkout
    /// selection off. The handler feeds all of the cart's line item ids into
    /// <c>CartAggregate.ChangeItemsSelectedAsync</c>, whose per-id <c>Items.FirstOrDefault</c> lookup
    /// makes the bulk selection update O(N²) in cart size — the path the singular
    /// <c>changeCartItemSelected</c> (one id) never exercises.</summary>
    public static ChangeAllCartItemsSelectedCommand CreateChangeAllCartItemsSelectedCommand()
    {
        var command = AbstractTypeFactory<ChangeAllCartItemsSelectedCommand>.TryCreateInstance();
        command.SelectedForCheckout = false;

        return WithCartContext(command);
    }

    /// <summary>A <c>removeCartItems</c> command removing EVERY line item of the loaded cart
    /// (<c>li-0</c>…<c>li-{count-1}</c>). <c>CartAggregate.RemoveItemsAsync</c> resolves the id list with
    /// a <c>Items.Where(ids.Contains)</c> scan plus a per-item <c>List.Remove</c>, so removing all N
    /// items is O(N²) in cart size — the bulk-remove path the singular <c>removeCartItem</c> never
    /// exercises.</summary>
    public static RemoveCartItemsCommand CreateRemoveCartItemsCommand(int lineItemCount)
    {
        var command = AbstractTypeFactory<RemoveCartItemsCommand>.TryCreateInstance();
        command.LineItemIds = Enumerable.Range(0, lineItemCount).Select(i => $"li-{i}").ToArray();

        return WithCartContext(command);
    }
}
