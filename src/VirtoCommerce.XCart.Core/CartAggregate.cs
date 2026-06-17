using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model.Search;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;
using StoreSetting = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;
using XCartSetting = VirtoCommerce.XCart.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XCart.Core
{
    [DebuggerDisplay("CartId = {Cart.Id}")]
    public class CartAggregate : Entity, IAggregateRoot, ICloneable
    {
        private readonly IMarketingPromoEvaluator _marketingEvaluator;
        private readonly IShoppingCartTotalsCalculator _cartTotalsCalculator;
        private readonly IOptionalDependency<ITaxProviderSearchService> _taxProviderSearchService;
        private readonly ICartProductService _cartProductService;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdaterService;
        private readonly IMemberService _memberService;
        private readonly IMapper _mapper;
        private readonly IGenericPipelineLauncher _pipeline;
        private readonly IConfigurationItemValidator _configurationItemValidator;
        private readonly IFileUploadService _fileUploadService;
        private readonly ICartSharingService _cartSharingService;
        private readonly ICartValidationContextFactory _cartValidationContextFactory;
        private readonly ICartItemBuilder _cartItemBuilder;

        private const char RuleSetSeparator = ',';

        public CartAggregate(
            IMarketingPromoEvaluator marketingEvaluator,
            IShoppingCartTotalsCalculator cartTotalsCalculator,
            IOptionalDependency<ITaxProviderSearchService> taxProviderSearchService,
            ICartProductService cartProductService,
            IDynamicPropertyUpdaterService dynamicPropertyUpdaterService,
            IMapper mapper,
            IMemberService memberService,
            IGenericPipelineLauncher pipeline,
            IConfigurationItemValidator configurationItemValidator,
            IFileUploadService fileUploadService,
            ICartSharingService cartSharingService,
            ICartValidationContextFactory cartValidationContextFactory,
            ICartItemBuilder cartItemBuilder)
        {
            _cartTotalsCalculator = cartTotalsCalculator;
            _marketingEvaluator = marketingEvaluator;
            _taxProviderSearchService = taxProviderSearchService;
            _cartProductService = cartProductService;
            _dynamicPropertyUpdaterService = dynamicPropertyUpdaterService;
            _mapper = mapper;
            _memberService = memberService;
            _pipeline = pipeline;
            _configurationItemValidator = configurationItemValidator;
            _fileUploadService = fileUploadService;
            _cartSharingService = cartSharingService;
            _cartValidationContextFactory = cartValidationContextFactory;
            _cartItemBuilder = cartItemBuilder;
        }

        public Store Store { get; protected set; }
        public Currency Currency { get; protected set; }
        public Member Member { get; protected set; }

        public IList<Currency> AllCurrencies { get; set; }

        public IList<CartTotalAggregate> CartTotals
        {
            get
            {
                var cartTotals = Cart.CartTotals?.Select(x => new CartTotalAggregate() { CartTotal = x }).ToList() ?? [];

                foreach (var item in cartTotals)
                {
                    item.IsDefaultTotalCurrency = Cart.Currency.EqualsIgnoreCase(item.CartTotal.CurrencyCode);
                    item.Currency = AllCurrencies?.FirstOrDefault(x => x.Code.EqualsIgnoreCase(item.CartTotal.CurrencyCode)) ?? Currency;
                    item.CartAggregate = this;
                }

                return cartTotals;
            }
        }

        public IEnumerable<CartCoupon> Coupons
        {
            get
            {
                var allAppliedCoupons = Cart.GetFlatObjectsListWithInterface<IHasDiscounts>()
                                            .SelectMany(x => x.Discounts ?? Array.Empty<Discount>())
                                            .Where(x => !string.IsNullOrEmpty(x.Coupon))
                                            .Select(x => x.Coupon)
                                            .Distinct()
                                            .ToList();

                foreach (var coupon in Cart.Coupons)
                {
                    var cartCoupon = new CartCoupon
                    {
                        Code = coupon,
                        IsAppliedSuccessfully = allAppliedCoupons.Any(c => c.EqualsIgnoreCase(coupon))
                    };
                    yield return cartCoupon;
                }
            }
        }

        public ShoppingCart Cart { get; protected set; }
        public IEnumerable<LineItem> GiftItems => Cart?.Items.Where(x => x.IsGift) ?? Enumerable.Empty<LineItem>();
        public IEnumerable<LineItem> LineItems => Cart?.Items.Where(x => !x.IsGift) ?? Enumerable.Empty<LineItem>();
        public IEnumerable<LineItem> SelectedLineItems => LineItems.Where(x => x.SelectedForCheckout);
        public IEnumerable<LineItem> CartCurrencySelectedLineItems => SelectedLineItems.Where(x => x.Currency.EqualsIgnoreCase(Cart.Currency) || x.Currency.IsNullOrEmpty());

        public bool HasSelectedLineItems => CartCurrencySelectedLineItems.Any();

        /// <summary>
        /// Represents the dictionary of all CartProducts data for line items and their configuration items.
        /// Key is a composite "{productId}:{CURRENCYCODE}" — built via <see cref="FormatGetCartProductKey(string, string)"/>;
        /// this allows storing the same product under different currencies in the same cart.
        /// Backed by <see cref="ConcurrentDictionary{TKey,TValue}"/> because cached aggregate instances
        /// are shared between concurrent readers without locking.
        /// </summary>
        public IDictionary<string, CartProduct> CartProducts { get; protected set; } = new ConcurrentDictionary<string, CartProduct>().WithDefaultValue(null);

        /// <summary>
        /// Builds a CartProducts dictionary key from product id and currency code.
        /// </summary>
        public static string FormatGetCartProductKey(string productId, string currencyCode)
        {
            return $"{productId}:{currencyCode}";
        }

        /// <summary>
        /// Builds a CartProducts dictionary key for the specified line item.
        /// Falls back to <see cref="ShoppingCart.Currency"/> when the line item has no currency set.
        /// </summary>
        public virtual string GetCartProductKey(LineItem lineItem)
        {
            var currencyCode = !string.IsNullOrEmpty(lineItem?.Currency) ? lineItem.Currency : Cart?.Currency;
            return FormatGetCartProductKey(lineItem?.ProductId, currencyCode);
        }

        public virtual string GetCartProductKey(string productId, string currencyCode)
        {
            var normalizedCode = !string.IsNullOrEmpty(currencyCode) ? currencyCode : Cart?.Currency;
            return FormatGetCartProductKey(productId, normalizedCode);
        }

        /// <summary>
        /// Resolves the <see cref="Currency"/> matching the specified code from <see cref="AllCurrencies"/>.
        /// Falls back to the cart's default <see cref="Currency"/> when no match is found.
        /// </summary>
        public virtual Currency GetCurrencyByCode(string currencyCode)
        {
            return AllCurrencies?.FirstOrDefault(x => x.Code.EqualsIgnoreCase(currencyCode)) ?? Currency;
        }

        /// <summary>
        /// Contains a new of validation rule set that will be executed each time the basket is changed.
        /// FluentValidation RuleSets allow you to group validation rules together which can be executed together as a group. You can set exists rule set name to evaluate default.
        /// <see cref="CartValidator"/>
        /// </summary>
        public string[] ValidationRuleSet { get; set; } =
        [
            ModuleConstants.ValidationRuleSets.Default,
            ModuleConstants.ValidationRuleSets.Strict
        ];

        /// <summary>
        /// Per-ruleSet validation results cache. Populated by <see cref="ValidateAsync(CartValidationContext, string)"/>.
        /// Cleared by <see cref="ClearValidationCache"/>.
        /// </summary>
        protected ConcurrentDictionary<string, IList<ValidationFailure>> ValidationErrorsByRuleSet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IsValid => ValidationErrorsByRuleSet.IsEmpty || ValidationErrorsByRuleSet.Values.All(x => x.Count == 0);

        [Obsolete("Use GetValidationErrors().", DiagnosticId = "VC0009", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public IList<ValidationFailure> ValidationErrors { get; protected set; } = new List<ValidationFailure>();

        public IList<ValidationFailure> OperationValidationErrors { get; protected set; } = new List<ValidationFailure>();

        [Obsolete("Use ValidationErrorsByRuleSet instead. This property only contains the last ValidateAsync call's results.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public IList<ValidationFailure> CartValidationErrors { get; protected set; } = new List<ValidationFailure>();

        [Obsolete("Use ValidationErrorsByRuleSet instead. The boolean flag does not track which ruleSet was validated.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public bool IsValidated { get; private set; }

        public IList<ValidationFailure> ValidationWarnings { get; protected set; } = new List<ValidationFailure>();

        [Obsolete("Use Cart.SharingSettings and ICartSharingService instead", false, DiagnosticId = "VC0011", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public virtual string Scope
        {
            get
            {
                return _cartSharingService.GetSharingScope(Cart);
            }
        }

        public IList<string> ProductsIncludeFields { get; set; }
        public string ResponseGroup { get; set; }

        public bool IsSelectedForCheckout => Store.Settings?.GetValue<bool>(XCartSetting.IsSelectedForCheckout) ?? true;

        /// <summary>
        /// Clears all cached validation results. Called after saving the cart
        /// so the mutation response and subsequent queries re-validate against the updated state.
        /// </summary>
        public void ClearValidationCache()
        {
            ValidationErrorsByRuleSet.Clear();
#pragma warning disable VC0015 // Obsolete: maintained for backward compatibility
            CartValidationErrors = new List<ValidationFailure>();
#pragma warning restore VC0015
        }

        /// <summary>
        /// Returns all cached validation errors across all rulesets that have been validated,
        /// combined with <see cref="OperationValidationErrors"/>. Does not trigger validation.
        /// </summary>
        public virtual IList<ValidationFailure> GetValidationErrors()
        {
#pragma warning disable VC0015 // Obsolete: maintained for backward compatibility
            return CartValidationErrors
                .Concat(OperationValidationErrors)
                .ToList();
#pragma warning restore VC0015
        }

        /// <summary>
        /// Re-validates the cart with the line-item ruleset and returns the validation errors for the
        /// specified line item. Used by the GraphQL per-line resolvers so the line-level
        /// validationErrors/isValid stay consistent with the cart-level resolver after a save clears the
        /// validation cache. Results are cached per ruleSet by <see cref="ValidateAsync(string)"/>.
        /// </summary>
        public virtual async Task<IList<CartValidationError>> GetLineItemValidationErrorsAsync(LineItem lineItem)
        {
            ArgumentNullException.ThrowIfNull(lineItem);

            var errors = await ValidateAsync(ModuleConstants.ValidationRuleSets.Items);

            return errors
                .Concat(OperationValidationErrors)
                .GetEntityCartErrors(lineItem)
                .ToList();
        }

        public virtual CartAggregate GrabCart(ShoppingCart cart, Store store, Member member, Currency currency)
        {
            Id = cart.Id;
            Cart = cart;
            Member = member;
            Currency = currency;
            Store = store;
            Cart.IsAnonymous = member == null;
            Cart.CustomerName = member?.Name ?? "Anonymous";
            Cart.Items ??= new List<LineItem>();

            return this;
        }

        public virtual Task<CartAggregate> UpdateCartComment(string comment)
        {
            EnsureCartExists();

            Cart.Comment = comment;

            return Task.FromResult(this);
        }

        /// <summary>
        /// Always add a new line item for a configured item.
        /// </summary>
        /// <param name="newCartItem"></param>
        /// <param name="newConfiguredItem"></param>
        /// <returns></returns>
        public virtual async Task<CartAggregate> AddConfiguredItemAsync(NewCartItem newCartItem, LineItem newConfiguredItem)
        {
            ArgumentNullException.ThrowIfNull(newCartItem);
            ArgumentNullException.ThrowIfNull(newConfiguredItem);

            var validationResult = await _configurationItemValidator.ValidateAsync(newConfiguredItem);
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);

                if (!newCartItem.IgnoreValidationErrors)
                {
                    return this;
                }
            }

            EnsureCartExists();

            if (newCartItem.CartProduct == null)
            {
                return this;
            }

            CartProducts[GetCartProductKey(newConfiguredItem)] = newCartItem.CartProduct;

            await PopulateCartProductsAsync(newConfiguredItem);

            newConfiguredItem.Id = null;
            newConfiguredItem.SelectedForCheckout = IsSelectedForCheckout;
            newConfiguredItem.Quantity = newCartItem.Quantity;
            newConfiguredItem.Note = newCartItem.Comment;

            await UpdateCreatedDate(newConfiguredItem, newCartItem);

            Cart.Items.Add(newConfiguredItem);

            if (newCartItem.DynamicProperties != null)
            {
                await UpdateCartItemDynamicProperties(newConfiguredItem, newCartItem.DynamicProperties);
            }

            await SetItemFulfillmentCenterAsync(newConfiguredItem, newCartItem.CartProduct);
            await UpdateVendor(newConfiguredItem, newCartItem.CartProduct);

            return this;
        }

        public virtual async Task<CartAggregate> AddItemAsync(NewCartItem newCartItem)
        {
            ArgumentNullException.ThrowIfNull(newCartItem);

            EnsureCartExists();

            var validationResult = await AbstractTypeFactory<NewCartItemValidator>.TryCreateInstance().ValidateAsync(newCartItem, options => options.IncludeRuleSets(ValidationRuleSet));
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);

                if (!newCartItem.IgnoreValidationErrors)
                {
                    return this;
                }
            }

            if (newCartItem.CartProduct == null)
            {
                return this;
            }

            if (newCartItem.IsWishlist && newCartItem.CartProduct.Price == null)
            {
                newCartItem.CartProduct.Price = new ProductPrice(Currency);
            }

            var lineItem = _mapper.Map<LineItem>(newCartItem.CartProduct, options =>
            {
                options.Items.TryAdd("cultureName", Cart.LanguageCode);
                options.Items.TryAdd("currencyCode", newCartItem.ItemCurrencyCode);
                options.Items.TryAdd(ICartItemBuilder.MapperContextKey, _cartItemBuilder);
            });

            lineItem.Currency ??= Currency.Code;
            lineItem.SelectedForCheckout = newCartItem.IsSelectedForCheckout ?? IsSelectedForCheckout;
            lineItem.Quantity = newCartItem.Quantity;

            await UpdateCreatedDate(lineItem, newCartItem);

            if (newCartItem.Price != null)
            {
                lineItem.ListPrice = newCartItem.Price.Value;
                lineItem.SalePrice = newCartItem.Price.Value;
            }
            else
            {
                SetLineItemTierPrice(newCartItem.CartProduct.Price, newCartItem.Quantity, lineItem);
            }

            if (!string.IsNullOrEmpty(newCartItem.Comment))
            {
                lineItem.Note = newCartItem.Comment;
            }

            CartProducts[GetCartProductKey(lineItem)] = newCartItem.CartProduct;
            await SetItemFulfillmentCenterAsync(lineItem, newCartItem.CartProduct);
            await UpdateVendor(lineItem, newCartItem.CartProduct);
            await InnerAddLineItemAsync(lineItem, newCartItem.OverrideQuantity, newCartItem.CartProduct, newCartItem.DynamicProperties);

            return this;
        }

        public virtual async Task<CartAggregate> AddItemsAsync(ICollection<NewCartItem> newCartItems)
        {
            EnsureCartExists();

            var productPairs = newCartItems.Select(x => (x.ItemCurrencyCode, x.ProductId)).Distinct().ToList();
            var products = await _cartProductService.GetCartProductsAsync(this, productPairs);

            foreach (var item in newCartItems)
            {
                var currencyCode = !string.IsNullOrEmpty(item.ItemCurrencyCode) ? item.ItemCurrencyCode : Currency.Code;
                if (!products.TryGetValue(GetCartProductKey(item.ProductId, currencyCode), out var product))
                {
                    var error = CartErrorDescriber.ProductUnavailableError(nameof(CatalogProduct), item.ProductId);
                    OperationValidationErrors.Add(error);
                }
                else
                {
                    item.CartProduct = product;
                    await AddItemAsync(item);
                }
            }

            return this;
        }

        public async Task<IEnumerable<GiftItem>> GetAvailableGiftsAsync(ICollection<PromotionReward> promotionRewards)
        {
            var giftRewards = promotionRewards
                            .OfType<GiftReward>()
                            .Where(reward => reward.IsValid)
                            // .Distinct() is needed as multiplied gifts would be returned otherwise.
                            .Distinct()
                            .ToArray();

            var productIds = giftRewards.Select(x => x.ProductId).Where(x => !x.IsNullOrEmpty()).Distinct().ToArray();
            if (productIds.Length == 0)
            {
                return new List<GiftItem>();
            }

            var productPairs = productIds.Select(id => (Currency.Code, id)).ToList();
            var products = await _cartProductService.GetCartProductsAsync(this, productPairs);

            var availableProductsIds = products.Values
                .Where(x => (x.Product.IsActive ?? false) &&
                            (x.Product.IsBuyable ?? false) &&
                            x.Price != null &&
                            (!(x.Product.TrackInventory ?? false) || x.AvailableQuantity >= giftRewards
                                .FirstOrDefault(y => y.ProductId == x.Product.Id)?.Quantity))
                .Select(x => x.Product.Id)
                .ToHashSet();

            return giftRewards
                .Where(x => x.ProductId.IsNullOrEmpty() || availableProductsIds.Contains(x.ProductId))
                .Select(reward =>
                {
                    var result = _mapper.Map<GiftItem>(reward);

                    // if reward has assigned product, add data from product
                    if (!string.IsNullOrEmpty(reward.ProductId) && products.TryGetValue(GetCartProductKey(reward.ProductId, Currency.Code), out var product))
                    {
                        result.CatalogId = product.Product.CatalogId;
                        result.CategoryId ??= product.Product.CategoryId;
                        result.ProductId = product.Product.Id;
                        result.Sku = product.Product.Code;
                        result.ImageUrl ??= product.Product.ImgSrc;
                        result.MeasureUnit ??= product.Product.MeasureUnit;
                        result.Name ??= product.GetName(Cart.LanguageCode);
                    }

                    var giftInCart = GiftItems.FirstOrDefault(x => x.EqualsReward(result));
                    // non-null LineItemId indicates that this GiftItem was added to the cart
                    if (giftInCart != null)
                    {
                        result.HasLineItem = true;

                        result.LineItemId = giftInCart.Id;
                        if (result.LineItemId == null && giftInCart is GiftLineItem giftLineItem)
                        {
                            result.LineItemId = giftLineItem.GiftItemId;
                        }

                        result.LineItemSelectedForCheckout = giftInCart.SelectedForCheckout;
                    }

                    // CacheKey as Id
                    result.Id = result.GetCacheKey();
                    return result;
                }).ToList();
        }

        public virtual Task<CartAggregate> AddGiftItemsAsync(IReadOnlyCollection<string> giftIds, IReadOnlyCollection<GiftItem> availableGifts)
        {
            EnsureCartExists();

            if (giftIds.IsNullOrEmpty())
            {
                return Task.FromResult(this);
            }

            foreach (var giftId in giftIds)
            {
                var availableGift = availableGifts.FirstOrDefault(x => x.Id == giftId);
                if (availableGift == null)
                {
                    // ignore the gift, if it's not in available gifts list
                    continue;
                }

                var giftItem = GiftItems.FirstOrDefault(x => x.EqualsReward(availableGift));
                if (giftItem == null)
                {
                    giftItem = _mapper.Map<GiftLineItem>(availableGift);

                    if (giftItem is GiftLineItem giftLineItem)
                    {
                        giftLineItem.GiftItemId = availableGift.Id;
                    }

                    giftItem.Id = null;
                    giftItem.IsGift = true;
                    giftItem.Discounts ??= new List<Discount>();

                    var item = AbstractTypeFactory<Discount>.TryCreateInstance();
                    item.Coupon = availableGift.Coupon;
                    item.PromotionId = availableGift.Promotion.Id;
                    item.Name = availableGift.Promotion.Name;
                    item.Description = availableGift.Promotion.Description;
                    item.Currency = Cart.Currency;

                    giftItem.Discounts.Add(item);
                    giftItem.CatalogId ??= "";
                    giftItem.ProductId ??= "";
                    giftItem.Sku ??= "";
                    giftItem.Currency = Currency.Code;
                    Cart.Items.Add(giftItem);
                }

                // always add gift items to the cart as selected for checkout
                giftItem.SelectedForCheckout = true;
                giftItem.IsRejected = false;
            }

            return Task.FromResult(this);
        }

        public virtual CartAggregate RejectCartItems(IReadOnlyCollection<string> cartItemIds)
        {
            EnsureCartExists();

            if (cartItemIds.IsNullOrEmpty())
            {
                return this;
            }

            foreach (var cartItemId in cartItemIds)
            {
                // cartItem If can be an unsaved gift as well
                var giftItem = GiftItems.FirstOrDefault(x => x.Id == cartItemId) ??
                               GiftItems.OfType<GiftLineItem>().FirstOrDefault(x => x.GiftItemId == cartItemId);

                if (giftItem != null)
                {
                    giftItem.SelectedForCheckout = false;
                    giftItem.IsRejected = true;
                }
            }

            return this;
        }

        public virtual async Task<CartAggregate> ChangeItemPriceAsync(PriceAdjustment priceAdjustment)
        {
            EnsureCartExists();

            var lineItem = Cart.Items.FirstOrDefault(x => x.Id == priceAdjustment.LineItemId);
            if (lineItem != null)
            {
                await AbstractTypeFactory<ChangeCartItemPriceValidator>.TryCreateInstance().ValidateAsync(priceAdjustment, options => options.IncludeRuleSets(ValidationRuleSet).ThrowOnFailures());
                lineItem.ListPrice = priceAdjustment.NewPrice;
                lineItem.SalePrice = priceAdjustment.NewPrice;
            }

            return this;
        }

        public virtual async Task<CartAggregate> ChangeItemQuantityAsync(ItemQtyAdjustment qtyAdjustment)
        {
            EnsureCartExists();

            var validationResult = await AbstractTypeFactory<ItemQtyAdjustmentValidator>.TryCreateInstance().ValidateAsync(qtyAdjustment, options => options.IncludeRuleSets(ValidationRuleSet));
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);
            }

            var lineItem = Cart.Items.FirstOrDefault(i => i.Id == qtyAdjustment.LineItemId);

            if (lineItem != null)
            {
                lineItem.Quantity = qtyAdjustment.NewQuantity;

                if (lineItem.IsConfigured)
                {
                    await UpdateConfiguredLineItemPrice([lineItem]);
                }
                else
                {
                    SetLineItemTierPrice(qtyAdjustment.CartProduct.Price, qtyAdjustment.NewQuantity, lineItem);
                }
            }

            return this;
        }

        public virtual Task<CartAggregate> ChangeItemCommentAsync(NewItemComment newItemComment)
        {
            EnsureCartExists();

            var lineItem = Cart.Items.FirstOrDefault(x => x.Id == newItemComment.LineItemId);
            if (lineItem != null)
            {
                lineItem.Note = newItemComment.Comment;
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> ChangeItemsSelectedAsync(IList<string> lineItemIds, bool selectedForCheckout)
        {
            EnsureCartExists();

            foreach (var lineItemId in lineItemIds)
            {
                var lineItem = Cart.Items.FirstOrDefault(x => x.Id == lineItemId);
                if (lineItem != null)
                {
                    lineItem.SelectedForCheckout = selectedForCheckout;
                }
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveItemAsync(string lineItemId)
        {
            EnsureCartExists();

            var lineItem = Cart.Items.FirstOrDefault(x => x.Id == lineItemId);
            if (lineItem != null)
            {
                Cart.Items.Remove(lineItem);
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveItemsAsync(string[] lineItemIds)
        {
            EnsureCartExists();

            var lineItems = Cart.Items.Where(x => lineItemIds.Contains(x.Id)).ToList();
            if (lineItems.Count != 0)
            {
                lineItems.ForEach(x => Cart.Items.Remove(x));
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveItemsByProductIdAsync(string productId)
        {
            EnsureCartExists();

            var lineItems = LineItems.Where(x => x.ProductId == productId).ToList();
            if (lineItems.Count != 0)
            {
                lineItems.ForEach(x => Cart.Items.Remove(x));
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveItemsByProductIdAndCurrencyAsync(string productId, string currencyCode)
        {
            EnsureCartExists();

            // Missing currencies on either side are treated as the cart's currency.
            var targetCurrency = !string.IsNullOrEmpty(currencyCode) ? currencyCode : Cart.Currency;
            var lineItems = LineItems.Where(x =>
                x.ProductId == productId &&
                (string.IsNullOrEmpty(x.Currency) ? Cart.Currency : x.Currency).EqualsIgnoreCase(targetCurrency)).ToList();

            if (lineItems.Count != 0)
            {
                lineItems.ForEach(x => Cart.Items.Remove(x));
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> AddCouponAsync(string couponCode)
        {
            EnsureCartExists();

            if (!Cart.Coupons.Any(c => c.EqualsIgnoreCase(couponCode)))
            {
                Cart.Coupons.Add(couponCode);
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveCouponAsync(string couponCode = null)
        {
            EnsureCartExists();
            if (string.IsNullOrEmpty(couponCode))
            {
                Cart.Coupons.Clear();
            }
            else
            {
                Cart.Coupons.Remove(Cart.Coupons.FirstOrDefault(c => c.EqualsIgnoreCase(couponCode)));
            }
            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> ClearAsync()
        {
            EnsureCartExists();

            await DeleteConfigurationFiles();

            Cart.Comment = string.Empty;
            Cart.PurchaseOrderNumber = string.Empty;
            Cart.Shipments.Clear();
            Cart.Payments.Clear();
            Cart.Addresses.Clear();

            Cart.Coupons.Clear();
            Cart.Items.Clear();
            Cart.DynamicProperties?.Clear();

            return this;
        }

        public virtual Task<CartAggregate> ChangePurchaseOrderNumber(string purchaseOrderNumber)
        {
            EnsureCartExists();

            Cart.PurchaseOrderNumber = purchaseOrderNumber;

            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> AddShipmentAsync(Shipment shipment, IEnumerable<ShippingRate> availRates)
        {
            EnsureCartExists();

            var validationContext = new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = availRates
            };
            await AbstractTypeFactory<CartShipmentValidator>.TryCreateInstance().ValidateAsync(validationContext, options => options.IncludeRuleSets(ValidationRuleSet).ThrowOnFailures());

            shipment.Currency = Cart.Currency;
            if (shipment.DeliveryAddress != null)
            {
                // Reset address key only if it doesn't belong to an existing shipment in this cart
                // This prevents PK duplication when using customer profile addresses across carts,
                // while preserving the key for addresses already associated with this cart's shipments
                var existingShipmentAddressKeys = Cart.Shipments
                    .Where(s => s.DeliveryAddress?.Key != null)
                    .Select(s => s.DeliveryAddress.Key)
                    .ToHashSet();

                if (!existingShipmentAddressKeys.Contains(shipment.DeliveryAddress.Key))
                {
                    shipment.DeliveryAddress.Key = null;
                }
            }

            await RemoveExistingShipmentAsync(shipment);
            Cart.Shipments.Add(shipment);

            if (availRates != null && !string.IsNullOrEmpty(shipment.ShipmentMethodCode) && !Cart.IsTransient())
            {
                var shippingMethod = availRates.First(sm => shipment.ShipmentMethodCode.EqualsIgnoreCase(sm.ShippingMethod.Code) && shipment.ShipmentMethodOption.EqualsIgnoreCase(sm.OptionName));
                shipment.Price = shippingMethod.Rate;
                shipment.DiscountAmount = shippingMethod.DiscountAmount;
            }

            // pass shipment to the middleware pipeline
            var shipmentContext = new ShipmentContextCartMap
            {
                CartAggregate = this,
                Shipment = shipment,
            };
            await _pipeline.Execute(shipmentContext);

            return this;
        }

        public virtual Task<CartAggregate> RemoveShipmentAsync(string shipmentId)
        {
            EnsureCartExists();

            var shipment = Cart.Shipments.FirstOrDefault(s => s.Id == shipmentId);
            if (shipment != null)
            {
                Cart.Shipments.Remove(shipment);
            }
            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> AddOrUpdateCartAddress(CartModule.Core.Model.Address address)
        {
            EnsureCartExists();
            //Remove existing address
            Cart.Addresses.Remove(address);
            Cart.Addresses.Add(address);

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> RemoveCartAddress(CartModule.Core.Model.Address address)
        {
            EnsureCartExists();
            //Remove existing address
            Cart.Addresses.Remove(address);

            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> AddPaymentAsync(Payment payment, IEnumerable<PaymentMethod> availPaymentMethods)
        {
            EnsureCartExists();
            var validationContext = new PaymentValidationContext
            {
                Payment = payment,
                AvailPaymentMethods = availPaymentMethods
            };
            await AbstractTypeFactory<CartPaymentValidator>.TryCreateInstance().ValidateAsync(validationContext, options => options.IncludeRuleSets(ValidationRuleSet).ThrowOnFailures());

            payment.Currency ??= Cart.Currency;
            await RemoveExistingPaymentAsync(payment);
            if (payment.BillingAddress != null)
            {
                //Reset address key because it can equal a customer address from profile and if not do that it may cause
                //address primary key duplication error for multiple carts with the same address
                payment.BillingAddress.Key = null;
            }

            Cart.Payments.Add(payment);

            return this;
        }

        public virtual Task<CartAggregate> AddOrUpdateCartAddressByTypeAsync(CartModule.Core.Model.Address address)
        {
            EnsureCartExists();

            //Reset address key because it can equal a customer address from profile and if not do that it may cause
            //address primary key duplication error for multiple carts with the same address
            address.Key = null;

            var existingAddress = Cart.Addresses.FirstOrDefault(x => x.AddressType == address.AddressType);

            if (existingAddress != null)
            {
                Cart.Addresses.Remove(existingAddress);
            }

            Cart.Addresses.Add(address);

            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> MergeWithCartAsync(CartAggregate otherCart)
        {
            EnsureCartExists();

            //Reset primary keys for all aggregated entities before merge
            //To prevent insertions same Ids for target cart
            //exclude user because it might be the current one
            var entities = otherCart.Cart.GetFlatObjectsListWithInterface<IEntity>();
            foreach (var entity in entities)
            {
                entity.Id = null;
            }

            await MergeLineItemsFromCartAsync(otherCart);
            await MergeCouponsFromCartAsync(otherCart);
            await MergeShipmentsFromCartAsync(otherCart);
            await MergePaymentsFromCartAsync(otherCart);

            return this;
        }

        protected virtual async Task MergeLineItemsFromCartAsync(CartAggregate otherCart)
        {
            foreach (var lineItem in otherCart.Cart.Items.ToList())
            {
                await InnerAddLineItemAsync(lineItem, overrideQuantity: false, product: otherCart.CartProducts[otherCart.GetCartProductKey(lineItem)]);
            }

            await PopulateCartProductsAsync(Cart.Items);
        }

        protected virtual async Task MergeCouponsFromCartAsync(CartAggregate otherCart)
        {
            foreach (var coupon in otherCart.Cart.Coupons.ToList())
            {
                await AddCouponAsync(coupon);
            }
        }

        protected virtual async Task MergeShipmentsFromCartAsync(CartAggregate otherCart)
        {
            // Do not copy shipments if there are already shipments in the cart
            if (Cart.Shipments.Count > 0)
            {
                return;
            }

            foreach (var shipment in otherCart.Cart.Shipments.ToList())
            {
                //Skip validation, do not pass avail methods
                await AddShipmentAsync(shipment, null);
            }
        }

        protected virtual async Task MergePaymentsFromCartAsync(CartAggregate otherCart)
        {
            // Do not copy payments if there are already payments in the cart
            if (Cart.Payments.Count > 0)
            {
                return;
            }

            foreach (var payment in otherCart.Cart.Payments.ToList())
            {
                //Skip validation, do not pass avail methods
                await AddPaymentAsync(payment, null);
            }
        }

        /// <summary>
        /// Validates the cart with the specified <paramref name="ruleSet"/>. Results are cached
        /// per ruleSet in <see cref="ValidationErrorsByRuleSet"/> — subsequent calls with the same
        /// ruleSet return cached results without re-running validation.
        /// <para>
        /// Delegates to <see cref="ValidateAsync(CartValidationContext, string)"/>, which remains the
        /// virtual extension point for derived aggregates during its deprecation window — overrides of
        /// that overload participate in every validation triggered through this method.
        /// </para>
        /// </summary>
        public virtual async Task<IList<ValidationFailure>> ValidateAsync(string ruleSet)
        {
            var key = NormalizeRuleSet(ruleSet);

            if (ValidationErrorsByRuleSet.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (_cartValidationContextFactory == null)
            {
                throw new InvalidOperationException(
                    $"Cannot validate: {nameof(ICartValidationContextFactory)} is not available. " +
                    $"Use the {nameof(CartAggregate)} constructor that accepts it.");
            }

            EnsureCartExists();

            var validationContext = await _cartValidationContextFactory.CreateValidationContextAsync(this);

#pragma warning disable VC0009 // Obsolete overload is intentionally kept as the virtual extension point
            return await ValidateAsync(validationContext, ruleSet);
#pragma warning restore VC0009
        }

        [Obsolete("Use ValidateAsync(string ruleSet). The context is now created internally.", DiagnosticId = "VC0009", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public virtual async Task<IList<ValidationFailure>> ValidateAsync(CartValidationContext validationContext, string ruleSet)
        {
            ArgumentNullException.ThrowIfNull(validationContext);

            var key = NormalizeRuleSet(ruleSet);

            if (ValidationErrorsByRuleSet.TryGetValue(key, out var cached))
            {
                return cached;
            }

            EnsureCartExists();

            validationContext.CartAggregate = this;

            var rules = ruleSet?.Split(RuleSetSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = await AbstractTypeFactory<CartValidator>.TryCreateInstance().ValidateAsync(validationContext, options => options.IncludeRuleSets(rules));

            ValidationErrorsByRuleSet[key] = result.Errors;
            CartValidationErrors = result.Errors;

            // Backward compatibility: keep obsolete flag in sync
            IsValidated = true;

            return result.Errors;
        }

        public virtual async Task<bool> ValidateCouponAsync(string coupon)
        {
            EnsureCartExists();

            var promotionResult = await EvaluatePromotionsAsync();
            if (promotionResult.Rewards == null)
            {
                return false;
            }

            var validCoupon = promotionResult.Rewards.FirstOrDefault(x => x.IsValid && x.Coupon.EqualsIgnoreCase(coupon));

            return validCoupon != null;
        }

        public virtual async Task<PromotionResult> EvaluatePromotionsAsync()
        {
            EnsureCartExists();

            var promotionResult = new PromotionResult();

            // Promotions are evaluated against the cart's main currency; skip when there are no items in it.
            if (HasSelectedLineItems && !CartCurrencySelectedLineItems.Any(i => i.IsReadOnly))
            {
                var evalContext = AbstractTypeFactory<PromotionEvaluationContext>.TryCreateInstance();
                var evalContextCartMap = AbstractTypeFactory<PromotionEvaluationContextCartMap>.TryCreateInstance();

                evalContextCartMap.CartAggregate = this;
                evalContextCartMap.PromotionEvaluationContext = evalContext;

                await _pipeline.Execute(evalContextCartMap);

                promotionResult = await EvaluatePromotionsAsync(evalContextCartMap.PromotionEvaluationContext);
            }

            return promotionResult;
        }

        public virtual Task<PromotionResult> EvaluatePromotionsAsync(PromotionEvaluationContext evalContext)
        {
            return _marketingEvaluator.EvaluatePromotionAsync(evalContext);
        }

        protected virtual async Task<IEnumerable<TaxRate>> EvaluateTaxesAsync()
        {
            EnsureCartExists();

            var result = Enumerable.Empty<TaxRate>();

            if (HasSelectedLineItems)
            {
                var taxProvider = await GetActiveTaxProviderAsync();
                if (taxProvider != null)
                {
                    var taxEvalContext = _mapper.Map<TaxEvaluationContext>(this);
                    result = taxProvider.CalculateRates(taxEvalContext);
                }
            }

            return result;
        }

        public virtual async Task<CartAggregate> RecalculateAsync()
        {
            EnsureCartExists();

            await UpdateOrganizationName();

            _cartTotalsCalculator.CalculateTotals(Cart);

            var promotionEvalResult = await EvaluatePromotionsAsync();
            await this.ApplyRewardsAsync(promotionEvalResult.Rewards);

            if (_taxProviderSearchService.HasValue)
            {
                var taxRates = await EvaluateTaxesAsync();
                Cart.ApplyTaxRates(taxRates);
            }

            _cartTotalsCalculator.CalculateTotals(Cart);
            return this;
        }

        public virtual Task<CartAggregate> SetItemFulfillmentCenterAsync(LineItem lineItem, CartProduct cartProduct)
        {
            lineItem.FulfillmentCenterId = cartProduct?.Inventory?.FulfillmentCenterId;
            lineItem.FulfillmentCenterName = cartProduct?.Inventory?.FulfillmentCenterName;

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> UpdateVendor(LineItem lineItem, CartProduct cartProduct)
        {
            lineItem.VendorId = cartProduct?.Product?.Vendor;

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> UpdateImageUrl(LineItem lineItem, CartProduct cartProduct)
        {
            if (cartProduct?.Product?.ImgSrc != null)
            {
                lineItem.ImageUrl = cartProduct.Product.ImgSrc;
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> UpdatePrices(LineItem lineItem, CartProduct cartProduct)
        {
            // update only partially loaded line items
            if (cartProduct?.Price != null && lineItem.PriceId == null)
            {
                lineItem.PriceId = cartProduct.Price.PricelistId;
                lineItem.Currency = cartProduct.Price.Currency.Code;
                lineItem.DiscountAmount = cartProduct.Price.DiscountAmount.InternalAmount;
                lineItem.SalePrice = cartProduct.Price.SalePrice.InternalAmount;
                lineItem.TaxDetails = cartProduct.Price.TaxDetails;
                lineItem.TaxPercentRate = cartProduct.Price.TaxPercentRate;
                lineItem.Discounts = cartProduct.Price.Discounts;
                lineItem.ListPrice = cartProduct.Price.ListPrice.InternalAmount;
            }

            return Task.FromResult(this);
        }

        public virtual Task<CartAggregate> UpdateProductName(LineItem lineItem, CartProduct cartProduct)
        {
            if (cartProduct?.Product != null)
            {
                lineItem.Name = cartProduct.GetName(Cart.LanguageCode);
            }

            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> UpdateOrganization(ShoppingCart cart, Member member)
        {
            if (member is Contact contact && cart.Type != CartType.Wishlist && cart.Type != CartType.SavedForLater)
            {
                cart.OrganizationId = contact.Organizations?.FirstOrDefault();

                if (!string.IsNullOrEmpty(cart.OrganizationId))
                {
                    var org = await _memberService.GetByIdAsync(cart.OrganizationId);
                    if (org != null)
                    {
                        cart.OrganizationName = org.Name;
                    }
                }
            }

            return this;
        }

        public virtual async Task<CartAggregate> UpdateOrganizationName()
        {
            if (string.IsNullOrEmpty(Cart.OrganizationId))
            {
                Cart.OrganizationName = null;
            }
            else if (string.IsNullOrEmpty(Cart.OrganizationName))
            {
                var organization = await _memberService.GetByIdAsync(Cart.OrganizationId);
                if (organization != null)
                {
                    Cart.OrganizationName = organization.Name;
                }
            }

            return this;
        }

        public virtual Task<CartAggregate> UpdateCreatedDate(LineItem lineItem, NewCartItem newCartItem)
        {
            if (newCartItem.CreatedDate != null)
            {
                lineItem.CreatedDate = newCartItem.CreatedDate.Value;
            }

            return Task.FromResult(this);
        }

        public virtual async Task<CartAggregate> UpdateCartDynamicProperties(IList<DynamicPropertyValue> dynamicProperties)
        {
            await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(Cart, dynamicProperties);

            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartItemDynamicProperties(string lineItemId, IList<DynamicPropertyValue> dynamicProperties)
        {
            var lineItem = Cart.Items.FirstOrDefault(x => x.Id == lineItemId);
            if (lineItem != null)
            {
                await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(lineItem, dynamicProperties);
            }

            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartItemDynamicProperties(LineItem lineItem, IList<DynamicPropertyValue> dynamicProperties)
        {
            await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(lineItem, dynamicProperties);
            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartShipmentDynamicProperties(string shipmentId, IList<DynamicPropertyValue> dynamicProperties)
        {
            var shipment = Cart.Shipments.FirstOrDefault(x => x.Id == shipmentId);
            if (shipment != null)
            {
                await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(shipment, dynamicProperties);
            }

            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartShipmentDynamicProperties(Shipment shipment, IList<DynamicPropertyValue> dynamicProperties)
        {
            await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(shipment, dynamicProperties);
            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartPaymentDynamicProperties(string paymentId, IList<DynamicPropertyValue> dynamicProperties)
        {
            var payment = Cart.Payments.FirstOrDefault(x => x.Id == paymentId);
            if (payment != null)
            {
                await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(payment, dynamicProperties);
            }

            return this;
        }

        public virtual async Task<CartAggregate> UpdateCartPaymentDynamicProperties(Payment payment, IList<DynamicPropertyValue> dynamicProperties)
        {
            await _dynamicPropertyUpdaterService.UpdateDynamicPropertyValues(payment, dynamicProperties);
            return this;
        }

        protected virtual Task<CartAggregate> RemoveExistingPaymentAsync(Payment payment)
        {
            if (payment != null)
            {
                var existingPayment = !payment.IsTransient() ? Cart.Payments.FirstOrDefault(s => s.Id == payment.Id) : null;
                if (existingPayment != null)
                {
                    Cart.Payments.Remove(existingPayment);
                }
            }

            return Task.FromResult(this);
        }

        protected virtual Task<CartAggregate> RemoveExistingShipmentAsync(Shipment shipment)
        {
            if (shipment != null)
            {
                // Get unique shipment from shipments by code/option pair or by id
                var existShipment = Cart.Shipments.FirstOrDefault(s => !shipment.IsTransient() && s.Id == shipment.Id);

                if (existShipment != null)
                {
                    Cart.Shipments.Remove(existShipment);
                }
            }

            return Task.FromResult(this);
        }

        protected virtual Task<CartAggregate> InnerChangeItemQuantityAsync(LineItem lineItem, int quantity, CartProduct product = null)
        {
            ArgumentNullException.ThrowIfNull(lineItem);

            if (!lineItem.IsReadOnly && product?.Price != null)
            {
                var tierPrice = product.Price.GetTierPrice(quantity);
                if (CheckPricePolicy(tierPrice))
                {
                    lineItem.SalePrice = tierPrice.ActualPrice.Amount;
                    lineItem.ListPrice = tierPrice.Price.Amount;
                }
            }
            if (quantity > 0)
            {
                lineItem.Quantity = quantity;
            }
            else
            {
                Cart.Items.Remove(lineItem);
            }
            return Task.FromResult(this);
        }

        /// <summary>
        /// Represents a price policy for a product. By default, product price should be greater than zero.
        /// </summary>
        protected virtual bool CheckPricePolicy(TierPrice tierPrice)
        {
            return tierPrice.Price.Amount > 0;
        }

        [Obsolete("Use InnerAddLineItemAsync(LineItem newLineItem, bool overrideQuantity) instead", DiagnosticId = "VC0011", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        protected virtual async Task<CartAggregate> InnerAddLineItemAsync(LineItem newLineItem, CartProduct product = null, IList<DynamicPropertyValue> dynamicProperties = null)
        {
            var existingLineItem = newLineItem.IsConfigured
                ? null
                : FindExistingLineItemBeforeAdd(newLineItem.ProductId, product, dynamicProperties);

            if (existingLineItem != null)
            {
                await InnerChangeItemQuantityAsync(existingLineItem, existingLineItem.Quantity + Math.Max(1, newLineItem.Quantity), product);

                existingLineItem.FulfillmentCenterId = newLineItem.FulfillmentCenterId;
                existingLineItem.FulfillmentCenterName = newLineItem.FulfillmentCenterName;

                newLineItem = existingLineItem;
            }
            else
            {
                newLineItem.Id = null;
                Cart.Items.Add(newLineItem);
            }

            if (dynamicProperties != null)
            {
                await UpdateCartItemDynamicProperties(newLineItem, dynamicProperties);
            }

            return this;
        }

        protected virtual async Task<CartAggregate> InnerAddLineItemAsync(LineItem newLineItem, bool overrideQuantity, CartProduct product = null, IList<DynamicPropertyValue> dynamicProperties = null)
        {
            var existingLineItem = newLineItem.IsConfigured
                ? null
                : FindExistingLineItemBeforeAdd(newLineItem, product, dynamicProperties);

            if (existingLineItem != null)
            {
                var newQuantity = overrideQuantity ? newLineItem.Quantity : existingLineItem.Quantity + Math.Max(1, newLineItem.Quantity);
                await InnerChangeItemQuantityAsync(existingLineItem, newQuantity, product);

                existingLineItem.FulfillmentCenterId = newLineItem.FulfillmentCenterId;
                existingLineItem.FulfillmentCenterName = newLineItem.FulfillmentCenterName;

                newLineItem = existingLineItem;
            }
            else
            {
                newLineItem.Id = null;
                Cart.Items.Add(newLineItem);
            }

            if (dynamicProperties != null)
            {
                await UpdateCartItemDynamicProperties(newLineItem, dynamicProperties);
            }

            return this;
        }

        /// <summary>
        /// Responsible for finding an existing line item before adding a new one.
        /// If method returns line item, it means that the new line item should be merged with the existing one.
        /// </summary>
        /// <param name="newProductId">new product id</param>
        /// <param name="newProduct">new product object</param>
        /// <param name="newDynamicProperties">new dynamic properties that should be added/updated in cart line item</param>
        /// <returns></returns>
        [Obsolete("Use FindExistingLineItemBeforeAdd.FindExistingLineItemBeforeAdd(LineItem newLineItem...) instead.", DiagnosticId = "VC0014")]
        protected virtual LineItem FindExistingLineItemBeforeAdd(string newProductId, CartProduct newProduct, IList<DynamicPropertyValue> newDynamicProperties)
        {
            return LineItems.FirstOrDefault(x => x.ProductId == newProductId && !x.IsConfigured);
        }

        /// <summary>
        /// Responsible for finding an existing line item before adding a new one.
        /// If method returns line item, it means that the new line item should be merged with the existing one.
        /// </summary>
        /// <param name="newLineItem">new line item</param>
        /// <param name="newProduct">new product object</param>
        /// <param name="newDynamicProperties">new dynamic properties that should be added/updated in cart line item</param>
        /// <returns></returns>
        protected virtual LineItem FindExistingLineItemBeforeAdd(LineItem newLineItem, CartProduct newProduct, IList<DynamicPropertyValue> newDynamicProperties)
        {
            // Missing currencies on either side are treated as the cart's currency.
            var newCurrency = !string.IsNullOrEmpty(newLineItem.Currency) ? newLineItem.Currency : Cart.Currency;
            return LineItems.FirstOrDefault(x =>
                x.ProductId == newLineItem.ProductId &&
                !x.IsConfigured &&
                (string.IsNullOrEmpty(x.Currency) ? Cart.Currency : x.Currency).EqualsIgnoreCase(newCurrency));
        }

        protected virtual void EnsureCartExists()
        {
            if (Cart == null)
            {
                throw new OperationCanceledException("Cart not loaded.");
            }
        }

        /// <summary>
        /// Normalizes a ruleSet string into a consistent cache key for <see cref="ValidationErrorsByRuleSet"/>.
        /// Sorts composite rulesets ("shipments,default" → "default,shipments") and collapses "*" variants.
        /// Protected so derived aggregates that post-process validation results can update
        /// the cache entry for a ruleSet under the same key the base class uses.
        /// <para>
        /// Note: composite keys like "default,shipments" and standalone "default" are cached separately
        /// even though the composite result is a superset. This is a deliberate design decision —
        /// resolving subset/superset relationships between rulesets would add complexity
        /// without meaningful performance benefit in practice.
        /// </para>
        /// </summary>
        protected static string NormalizeRuleSet(string ruleSet)
        {
            if (string.IsNullOrEmpty(ruleSet))
            {
                return ModuleConstants.ValidationRuleSets.Default;
            }

            // "*" subsumes all other rulesets — no need to split
            if (ruleSet.Contains('*'))
            {
                return "*";
            }

            // Single token (no comma): no normalization needed.
            // The dictionary comparer is OrdinalIgnoreCase, so case collapses at lookup time.
            if (ruleSet.IndexOf(RuleSetSeparator) < 0)
            {
                return ruleSet;
            }

            var parts = ruleSet.Split(RuleSetSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 1)
            {
                return parts[0];
            }

            // Sort for consistent cache keys: "shipments,default" == "default,shipments"
            Array.Sort(parts, StringComparer.OrdinalIgnoreCase);
            return string.Join(RuleSetSeparator, parts);
        }

        protected virtual async Task<TaxProvider> GetActiveTaxProviderAsync()
        {
            if (Store?.Settings?.GetValue<bool>(StoreSetting.TaxCalculationEnabled) != true)
            {
                return null;
            }

            if (!_taxProviderSearchService.HasValue)
            {
                return null;
            }

            var taxProviderSearchCriteria = AbstractTypeFactory<TaxProviderSearchCriteria>.TryCreateInstance();
            taxProviderSearchCriteria.StoreIds = [Cart.StoreId];
            var storeTaxProviders = await _taxProviderSearchService.Value.SearchAsync(taxProviderSearchCriteria);

            return storeTaxProviders?.Results.FirstOrDefault(x => x.IsActive);
        }

        /// <summary>
        /// Sets ListPrice and SalePrice for line item by Product price
        /// </summary>
        public virtual void SetLineItemTierPrice(ProductPrice productPrice, int quantity, LineItem lineItem)
        {
            if (productPrice == null)
            {
                return;
            }

            var tierPrice = productPrice.GetTierPrice(quantity);
            if (tierPrice.Price.Amount > 0)
            {
                lineItem.SalePrice = tierPrice.ActualPrice.Amount;
                lineItem.ListPrice = tierPrice.Price.Amount;
            }
        }

        public virtual async Task<CartAggregate> UpdateConfiguredLineItemAsync(string lineItemId, LineItem configuredItem)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configuredItem);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);

            if (lineItem != null)
            {
                var validationResult = await _configurationItemValidator.ValidateAsync(configuredItem);
                if (!validationResult.IsValid)
                {
                    OperationValidationErrors.AddRange(validationResult.Errors);
                    return this;
                }

                lineItem.Quantity = configuredItem.Quantity;
                lineItem.ListPrice = configuredItem.ListPrice;
                lineItem.SalePrice = configuredItem.SalePrice;
                lineItem.DiscountAmount = configuredItem.DiscountAmount;
                lineItem.PlacedPrice = configuredItem.PlacedPrice;
                lineItem.ExtendedPrice = configuredItem.ExtendedPrice;

                // Delete files that are not present in the updated configuration
                var fileUrls = lineItem.GetConfigurationFileUrls()
                    .Except(configuredItem.GetConfigurationFileUrls())
                    .ToArray();

                await DeleteConfigurationFiles(fileUrls);

                lineItem.ConfigurationItems = configuredItem.ConfigurationItems.ToList();

                await PopulateCartProductsAsync(lineItem);
            }

            return this;
        }

        public virtual Task<CartAggregate> AddConfigurationItemAsync(string lineItemId, ProductConfigurationSection configurationSection)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSection);

            EnsureCartExists();

            return AddConfigurationItemsAsync(lineItemId, [configurationSection]);
        }

        public virtual Task<CartAggregate> AddConfigurationItemsAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSections);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);
            if (lineItem is null)
            {
                OperationValidationErrors.Add(CartErrorDescriber.ConfiguredLineItemNotFound(lineItemId));

                return Task.FromResult(this);
            }

            return AddConfigurationItemsAsync(lineItem, configurationSections);
        }

        protected virtual async Task<CartAggregate> AddConfigurationItemsAsync(LineItem lineItem, IList<ProductConfigurationSection> configurationSections)
        {
            ValidateConfigurationSections(lineItem, configurationSections);
            if (OperationValidationErrors.Any())
            {
                return this;
            }

            var cloneItem = lineItem.CloneTyped();
            cloneItem.ConfigurationItems ??= new List<ConfigurationItem>();

            await PopulateCartProductsAsync(configurationSections, lineItem.Currency);

            foreach (var configurationSection in configurationSections)
            {
                await ApplyConfigurationSectionAsync(cloneItem, configurationSection);
            }

            if (OperationValidationErrors.Any())
            {
                return this;
            }

            var validationResult = await _configurationItemValidator.ValidateAsync(cloneItem);
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);
                return this;
            }

            lineItem.ConfigurationItems = cloneItem.ConfigurationItems;

            await UpdateConfiguredLineItemPrice([lineItem]);

            return this;
        }

        public virtual Task<CartAggregate> UpdateConfigurationItemAsync(string lineItemId, ProductConfigurationSection configurationSection)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSection);

            EnsureCartExists();

            return UpdateConfigurationItemsAsync(lineItemId, [configurationSection]);
        }

        public virtual Task<CartAggregate> UpdateConfigurationItemsAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSections);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);
            if (lineItem is null)
            {
                OperationValidationErrors.Add(CartErrorDescriber.ConfiguredLineItemNotFound(lineItemId));

                return Task.FromResult(this);
            }

            return UpdateConfigurationItemsAsync(lineItem, configurationSections);
        }

        protected virtual async Task<CartAggregate> UpdateConfigurationItemsAsync(LineItem lineItem, IList<ProductConfigurationSection> configurationSections)
        {
            ValidateConfigurationSections(lineItem, configurationSections);
            if (OperationValidationErrors.Any())
            {
                return this;
            }

            var cloneItem = lineItem.CloneTyped();
            cloneItem.ConfigurationItems ??= new List<ConfigurationItem>();

            await PopulateCartProductsAsync(configurationSections, lineItem.Currency);

            var fileUrlsToDelete = new List<string>();

            // Update or create configuration items
            foreach (var configurationSection in configurationSections)
            {
                await ApplyConfigurationSectionAsync(cloneItem, configurationSection, fileUrlsToDelete);
            }

            if (OperationValidationErrors.Any())
            {
                return this;
            }

            var validationResult = await _configurationItemValidator.ValidateAsync(cloneItem);
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);
                return this;
            }

            lineItem.ConfigurationItems = cloneItem.ConfigurationItems;

            if (fileUrlsToDelete.Count > 0)
            {
                await DeleteConfigurationFiles(fileUrlsToDelete);
            }

            await UpdateConfiguredLineItemPrice([lineItem]);

            return this;
        }

        protected virtual async Task<ConfigurationItem> ApplyConfigurationSectionAsync(
            LineItem lineItem,
            ProductConfigurationSection configurationSection,
            IList<string> fileUrlsToDelete = null)
        {
            ConfigurationItem configurationItem = null;
            switch (configurationSection.Type)
            {
                case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation when !string.IsNullOrEmpty(configurationSection.Option?.ProductId):
                    var cartProduct = CartProducts[GetCartProductKey(configurationSection.Option.ProductId, lineItem.Currency)];
                    if (cartProduct is null)
                    {
                        OperationValidationErrors.Add(CartErrorDescriber.ProductUnavailableError(nameof(CatalogProduct), configurationSection.Option.ProductId));
                        return null;
                    }

                    configurationItem = GetOrCreateConfigurationItem(lineItem, configurationSection, cartProduct);
                    UpdateConfigurationItemForProduct(configurationItem, configurationSection, cartProduct);

                    break;

                case ConfigurationSectionTypeText:
                    configurationItem = GetOrCreateConfigurationItem(lineItem, configurationSection);
                    UpdateConfigurationItemForText(configurationItem, configurationSection);

                    break;

                case ConfigurationSectionTypeFile:
                    configurationItem = GetOrCreateConfigurationItem(lineItem, configurationSection);
                    if (fileUrlsToDelete != null && !configurationItem.Files.IsNullOrEmpty())
                    {
                        foreach (var url in configurationItem.Files.Select(x => x.Url).Except(configurationSection.FileUrls))
                        {
                            fileUrlsToDelete.Add(url);
                        }
                    }

                    await UpdateConfigurationItemForFilesAsync(configurationItem, configurationSection);

                    break;
            }

            return configurationItem;
        }

        public virtual Task<CartAggregate> ChangeConfigurationItemSelectedAsync(string lineItemId, ProductConfigurationSection configurationSection, bool selectedForCheckout)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSection);

            EnsureCartExists();

            return ChangeConfigurationItemsSelectedAsync(lineItemId, [configurationSection], selectedForCheckout);
        }

        public virtual Task<CartAggregate> ChangeConfigurationItemsSelectedAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections, bool selectedForCheckout)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSections);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);
            if (lineItem is null)
            {
                OperationValidationErrors.Add(CartErrorDescriber.ConfiguredLineItemNotFound(lineItemId));

                return Task.FromResult(this);
            }

            return ChangeConfigurationItemsSelectedAsync(lineItem, configurationSections, selectedForCheckout);
        }

        protected virtual async Task<CartAggregate> ChangeConfigurationItemsSelectedAsync(LineItem lineItem, IList<ProductConfigurationSection> configurationSections, bool selectedForCheckout)
        {
            if (lineItem.ConfigurationItems.IsNullOrEmpty() || configurationSections.IsNullOrEmpty())
            {
                return this;
            }

            var changed = false;
            foreach (var configurationSection in configurationSections)
            {
                var configurationItem = FindConfigurationItem(lineItem, configurationSection);
                if (configurationItem is not null && configurationItem.SelectedForCheckout != selectedForCheckout)
                {
                    configurationItem.SelectedForCheckout = selectedForCheckout;
                    changed = true;
                }
            }

            if (changed)
            {
                await UpdateConfiguredLineItemPrice([lineItem]);
            }

            return this;
        }

        public virtual Task<CartAggregate> ChangeAllConfigurationItemsSelectedAsync(string lineItemId, bool selectedForCheckout)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);
            if (lineItem is null)
            {
                OperationValidationErrors.Add(CartErrorDescriber.ConfiguredLineItemNotFound(lineItemId));

                return Task.FromResult(this);
            }

            return ChangeAllConfigurationItemsSelectedAsync(lineItem, selectedForCheckout);
        }

        protected virtual async Task<CartAggregate> ChangeAllConfigurationItemsSelectedAsync(LineItem lineItem, bool selectedForCheckout)
        {
            if (lineItem.ConfigurationItems.IsNullOrEmpty())
            {
                return this;
            }

            var changed = false;
            foreach (var configurationItem in lineItem.ConfigurationItems)
            {
                if (configurationItem.SelectedForCheckout != selectedForCheckout)
                {
                    configurationItem.SelectedForCheckout = selectedForCheckout;
                    changed = true;
                }
            }

            if (changed)
            {
                await UpdateConfiguredLineItemPrice([lineItem]);
            }

            return this;
        }

        public virtual Task<CartAggregate> RemoveConfigurationItemAsync(string lineItemId, ProductConfigurationSection configurationSection)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSection);

            return RemoveConfigurationItemsAsync(lineItemId, [configurationSection]);
        }

        public virtual async Task<CartAggregate> RemoveConfigurationItemsAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections)
        {
            ArgumentNullException.ThrowIfNull(lineItemId);
            ArgumentNullException.ThrowIfNull(configurationSections);

            EnsureCartExists();

            var lineItem = GetConfiguredLineItem(lineItemId);
            if (lineItem is null)
            {
                OperationValidationErrors.Add(CartErrorDescriber.ConfiguredLineItemNotFound(lineItemId));
                return this;
            }

            return await RemoveConfigurationItemsAsync(lineItem, configurationSections);
        }

        protected virtual async Task<CartAggregate> RemoveConfigurationItemsAsync(LineItem lineItem, IList<ProductConfigurationSection> configurationSections)
        {
            ValidateConfigurationSections(lineItem, configurationSections);
            if (OperationValidationErrors.Any())
            {
                return this;
            }

            var cloneItem = lineItem.CloneTyped();
            cloneItem.ConfigurationItems ??= new List<ConfigurationItem>();

            var fileUrlsToDelete = new List<string>();

            // Find and remove matching items from clone
            foreach (var configurationSection in configurationSections)
            {
                var configurationItem = FindConfigurationItem(cloneItem, configurationSection);
                if (configurationItem is null)
                {
                    // Already removed - no error (idempotent delete)
                    continue;
                }

                cloneItem.ConfigurationItems.Remove(configurationItem);

                // Collect file URLs for deferred deletion
                if (configurationItem.Type is ConfigurationSectionTypeFile && !configurationItem.Files.IsNullOrEmpty())
                {
                    fileUrlsToDelete.AddRange(configurationItem.Files.Select(x => x.Url));
                }
            }

            var validationResult = await _configurationItemValidator.ValidateAsync(cloneItem);
            if (!validationResult.IsValid)
            {
                OperationValidationErrors.AddRange(validationResult.Errors);
                return this;
            }

            lineItem.ConfigurationItems = cloneItem.ConfigurationItems;

            if (fileUrlsToDelete.Count > 0)
            {
                await DeleteConfigurationFiles(fileUrlsToDelete);
            }

            await UpdateConfiguredLineItemPrice([lineItem]);

            return this;
        }

        protected virtual void ValidateConfigurationSections(LineItem lineItem, IList<ProductConfigurationSection> configurationSections)
        {
            foreach (var configurationSection in configurationSections)
            {
                switch (configurationSection.Type)
                {
                    case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation:
                        if (string.IsNullOrEmpty(configurationSection.Option?.ProductId))
                        {
                            OperationValidationErrors.Add(CartErrorDescriber.SelectedProductIsRequired(lineItem));
                        }
                        break;

                    case ConfigurationSectionTypeText:
                    case ConfigurationSectionTypeFile:
                        break;

                    default:
                        OperationValidationErrors.Add(CartErrorDescriber.ConfigurationSectionUnknownType(lineItem, configurationSection.Type, configurationSection.SectionId));
                        break;
                }
            }
        }

        protected virtual ConfigurationItem GetOrCreateConfigurationItem(LineItem lineItem, ProductConfigurationSection configurationSection, CartProduct cartProduct = null)
        {
            var configurationItem = FindConfigurationItem(lineItem, configurationSection);
            if (configurationItem is null)
            {
                configurationItem = CreateConfigurationItem(configurationSection, cartProduct);
                lineItem.ConfigurationItems ??= new List<ConfigurationItem>();
                lineItem.ConfigurationItems.Add(configurationItem);
            }

            if (!string.IsNullOrEmpty(configurationSection.SectionName))
            {
                configurationItem.SectionName = configurationSection.SectionName;
            }

            return configurationItem;
        }

        protected virtual ConfigurationItem FindConfigurationItem(LineItem lineItem, ProductConfigurationSection configurationSection)
        {
            return configurationSection.Type is ConfigurationSectionTypeVariation
                // For Variation: search by Type + SectionId + ProductId (multiple variations can exist)
                ? lineItem.ConfigurationItems?.FirstOrDefault(x =>
                    x.Type == configurationSection.Type && x.SectionId == configurationSection.SectionId && x.ProductId == configurationSection.Option?.ProductId)
                // For Product, Text and File: search only by Type + SectionId
                : lineItem.ConfigurationItems?.FirstOrDefault(x =>
                    x.Type == configurationSection.Type && x.SectionId == configurationSection.SectionId);
        }

        protected virtual ConfigurationItem CreateConfigurationItem(ProductConfigurationSection configurationSection, CartProduct cartProduct)
        {
            return _cartItemBuilder.Create(configurationSection, cartProduct);
        }

        protected virtual void UpdateConfigurationItemForProduct(ConfigurationItem item, ProductConfigurationSection configurationSection, CartProduct cartProduct)
        {
            item.ProductId = configurationSection.Option.ProductId;
            item.Quantity = configurationSection.Option.Quantity;
            item.SelectedForCheckout = configurationSection.Option.SelectedForCheckout;
            item.Name = cartProduct.GetName(Cart.LanguageCode);
            item.Sku = cartProduct.Product.Code;
            item.ImageUrl = cartProduct.Product.ImgSrc;
            item.CatalogId = cartProduct.Product.CatalogId;
            item.CategoryId = cartProduct.Product.CategoryId;

            if (cartProduct.Price != null)
            {
                var tierPrice = cartProduct.Price.GetTierPrice(configurationSection.Option.Quantity);
                item.ListPrice = tierPrice.Price.Amount;
                item.SalePrice = tierPrice.ActualPrice.Amount;
            }
        }

        protected virtual void UpdateConfigurationItemForText(ConfigurationItem item, ProductConfigurationSection configurationSection)
        {
            item.CustomText = configurationSection.CustomText;
        }

        protected virtual async Task UpdateConfigurationItemForFilesAsync(ConfigurationItem item, ProductConfigurationSection configurationSection)
        {
            item.Files = await GetConfigurationFilesAsync(configurationSection.FileUrls);
        }

        protected virtual async Task<IList<ConfigurationItemFile>> GetConfigurationFilesAsync(IList<string> fileUrls)
        {
            if (fileUrls.IsNullOrEmpty())
            {
                return [];
            }

            return (await _fileUploadService.GetByPublicUrlAsync(fileUrls))
                .Where(x => x.Scope == ConfigurationSectionFilesScope && (x.OwnerIsEmpty() || x.OwnerIs(Cart)))
                .Select(x => x.ConvertToConfigurationItemFile())
                .ToList();
        }

        public virtual LineItem GetConfiguredLineItem(string lineItemId)
        {
            return Cart.Items.FirstOrDefault(x => x.Id == lineItemId && x.IsConfigured);
        }

        public virtual async Task<CartAggregate> UpdateConfiguredLineItemPrice(IList<LineItem> configuredItems)
        {
            await PopulateCartProductsAsync(configuredItems);

            foreach (var configurationLineItem in configuredItems)
            {
                var container = CreateConfiguredLineItemContainer(configurationLineItem);
                container.UpdatePrice(configurationLineItem);
                container.SyncConfigurationPrices(configurationLineItem);
            }

            return this;
        }

        /// <summary>
        /// Loads <see cref="CartProduct"/>s for the given (currency, productId) pairs that are not already
        /// in <see cref="CartProducts"/>, and writes them into <see cref="CartProducts"/> keyed by the
        /// composite cart-product key (see <see cref="FormatGetCartProductKey(string, string)"/>).
        /// </summary>
        public virtual async Task PopulateCartProductsAsync(IList<(string CurrencyCode, string ProductId)> productKeys)
        {
            if (productKeys.IsNullOrEmpty())
            {
                return;
            }

            var missingKeys = productKeys
                .Where(x => !string.IsNullOrEmpty(x.ProductId) && !CartProducts.ContainsKey(GetCartProductKey(x.ProductId, x.CurrencyCode)))
                .Distinct()
                .ToList();
            if (missingKeys.Count == 0)
            {
                return;
            }

            var products = await _cartProductService.GetCartProductsAsync(this, missingKeys);
            foreach (var product in products)
            {
                CartProducts[product.Key] = product.Value;
            }
        }

        /// <summary>
        /// Loads <see cref="CartProduct"/>s referenced by the given line items and their configuration items into
        /// <see cref="CartProducts"/>, each keyed under the owning line item's currency. Skips items with empty ProductId.
        /// </summary>
        protected virtual Task PopulateCartProductsAsync(ICollection<LineItem> lineItems)
        {
            if (lineItems.IsNullOrEmpty())
            {
                return Task.CompletedTask;
            }

            var productKeys = lineItems.Select(x => (x.Currency, x.ProductId))
                .Concat(lineItems
                    .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                    .SelectMany(x => x.ConfigurationItems, (lineItem, configurationItem) => (lineItem.Currency, configurationItem.ProductId)))
                .Where(x => !string.IsNullOrEmpty(x.ProductId))
                .Distinct()
                .ToList();

            return PopulateCartProductsAsync(productKeys);
        }

        /// <summary>
        /// Loads <see cref="CartProduct"/>s referenced by the line item and its configuration items into
        /// <see cref="CartProducts"/>, keyed under the line item's currency.
        /// </summary>
        protected virtual Task PopulateCartProductsAsync(LineItem lineItem)
        {
            return lineItem is null ? Task.CompletedTask : PopulateCartProductsAsync([lineItem]);
        }

        /// <summary>
        /// Loads <see cref="CartProduct"/>s referenced by Product/Variation-typed configuration
        /// sections into <see cref="CartProducts"/> under <paramref name="currencyCode"/>. Text and File
        /// sections are skipped — they don't reference catalog products.
        /// </summary>
        protected virtual Task PopulateCartProductsAsync(ICollection<ProductConfigurationSection> configurationSections, string currencyCode)
        {
            if (configurationSections.IsNullOrEmpty())
            {
                return Task.CompletedTask;
            }

            var productKeys = configurationSections
                .Where(x => !string.IsNullOrEmpty(x.Option?.ProductId))
                .Select(x => (currencyCode, x.Option.ProductId))
                .Distinct()
                .ToList();

            return PopulateCartProductsAsync(productKeys);
        }

        protected virtual ConfiguredLineItemContainer CreateConfiguredLineItemContainer(LineItem configurationLineItem)
        {
            var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
            container.Currency = GetCurrencyByCode(configurationLineItem.Currency);
            container.Store = Store;
            container.CartItemBuilder = _cartItemBuilder;
            container.ConfigurableProduct = CartProducts[GetCartProductKey(configurationLineItem)];

            foreach (var configurationItem in configurationLineItem.ConfigurationItems ?? [])
            {
                switch (configurationItem.Type)
                {
                    case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation when !string.IsNullOrEmpty(configurationItem.ProductId):
                        if (CartProducts[GetCartProductKey(configurationItem.ProductId, configurationLineItem.Currency)] is { } product)
                        {
                            container.AddProductSectionLineItem(product, configurationItem);
                        }

                        break;

                    case ConfigurationSectionTypeText:
                        container.AddTextSectionLineItem(configurationItem);
                        break;

                    case ConfigurationSectionTypeFile:
                        container.AddFileSectionLineItem(configurationItem);
                        break;
                }
            }

            return container;
        }

        [Obsolete("Use ConfiguredLineItemContainer.SyncConfigurationPrices instead.", DiagnosticId = "VC0010")]
        protected virtual void SyncConfigurationItemPrices(LineItem configurationLineItem, ExpConfigurationLineItem recalculated)
        {
            if (recalculated.Item?.ConfigurationItems.IsNullOrEmpty() != false)
            {
                return;
            }

            foreach (var recalculatedItem in recalculated.Item.ConfigurationItems)
            {
                var existingItem = configurationLineItem.ConfigurationItems?.FirstOrDefault(x =>
                    x.Type == recalculatedItem.Type &&
                    x.SectionId == recalculatedItem.SectionId &&
                    (recalculatedItem.Type != ConfigurationSectionTypeVariation || x.ProductId == recalculatedItem.ProductId));

                if (existingItem != null)
                {
                    existingItem.ListPrice = recalculatedItem.ListPrice;
                    existingItem.SalePrice = recalculatedItem.SalePrice;
                }
            }
        }

        protected virtual Task DeleteConfigurationFiles()
        {
            var fileUrls = Cart.Items
                .SelectMany(x => x.GetConfigurationFileUrls())
                .Distinct()
                .ToArray();

            return DeleteConfigurationFiles(fileUrls);
        }

        protected virtual async Task DeleteConfigurationFiles(IList<string> fileUrls)
        {
            if (fileUrls.IsNullOrEmpty())
            {
                return;
            }

            var files = (await _fileUploadService.GetByPublicUrlAsync(fileUrls))
                .Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIs(Cart))
                .ToList();

            if (files.Count > 0)
            {
                var fileIds = files.Select(x => x.Id).ToArray();
                await _fileUploadService.DeleteAsync(fileIds);
            }
        }

        #region ICloneable

        public virtual object Clone()
        {
            var result = (CartAggregate)MemberwiseClone();

            result.Cart = Cart?.CloneTyped();
            result.CartProducts = new ConcurrentDictionary<string, CartProduct>(
                    CartProducts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.CloneTyped()))
                .WithDefaultValue(null);
            result.Currency = Currency.CloneTyped();
            result.Member = Member?.CloneTyped();
            result.Store = Store.CloneTyped();

            // Re-create mutable collections so the clone doesn't share references with the original.
            // MemberwiseClone copies references — writes/clears on one instance would leak to the other.
            result.ValidationErrorsByRuleSet = new ConcurrentDictionary<string, IList<ValidationFailure>>(ValidationErrorsByRuleSet, StringComparer.OrdinalIgnoreCase);
#pragma warning disable VC0015 // Obsolete: maintained for backward compatibility
            result.CartValidationErrors = new List<ValidationFailure>(CartValidationErrors);
#pragma warning restore VC0015
            result.OperationValidationErrors = new List<ValidationFailure>(OperationValidationErrors);
            result.ValidationWarnings = new List<ValidationFailure>(ValidationWarnings);

            return result;
        }

        #endregion ICloneable
    }
}
