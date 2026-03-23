using System;
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
using VirtoCommerce.Platform.Core.Extensions;
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
            ICartSharingService cartSharingService)
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
        }

        public Store Store { get; protected set; }
        public Currency Currency { get; protected set; }
        public Member Member { get; protected set; }

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
                        IsAppliedSuccessfully = allAppliedCoupons.Contains(coupon)
                    };
                    yield return cartCoupon;
                }
            }
        }

        public ShoppingCart Cart { get; protected set; }
        public IEnumerable<LineItem> GiftItems => Cart?.Items.Where(x => x.IsGift) ?? Enumerable.Empty<LineItem>();
        public IEnumerable<LineItem> LineItems => Cart?.Items.Where(x => !x.IsGift) ?? Enumerable.Empty<LineItem>();
        public IEnumerable<LineItem> SelectedLineItems => LineItems.Where(x => x.SelectedForCheckout);

        public bool HasSelectedLineItems => SelectedLineItems.Any();

        /// <summary>
        /// Represents the dictionary of all CartProducts data for each  existing cart line item
        /// </summary>
        public IDictionary<string, CartProduct> CartProducts { get; protected set; } = new Dictionary<string, CartProduct>().WithDefaultValue(null);

        /// <summary>
        /// Contains a new of validation rule set that will be executed each time the basket is changed.
        /// FluentValidation RuleSets allow you to group validation rules together which can be executed together as a group. You can set exists rule set name to evaluate default.
        /// <see cref="CartValidator"/>
        /// </summary>
        public string[] ValidationRuleSet { get; set; } = { "default", "strict" };

        public bool IsValid => !GetValidationErrors().Any();

        [Obsolete("Use GetValidationErrors().", DiagnosticId = "VC0009", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        public IList<ValidationFailure> ValidationErrors { get; protected set; } = new List<ValidationFailure>();

        public IList<ValidationFailure> OperationValidationErrors { get; protected set; } = new List<ValidationFailure>();
        public IList<ValidationFailure> CartValidationErrors { get; protected set; } = new List<ValidationFailure>();

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

        public virtual IList<ValidationFailure> GetValidationErrors()
        {
            return CartValidationErrors.Concat(OperationValidationErrors).ToList();
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

            CartProducts[newCartItem.CartProduct.Id] = newCartItem.CartProduct;

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

            var lineItem = _mapper.Map<LineItem>(newCartItem.CartProduct, options => options.Items.TryAdd("cultureName", Cart.LanguageCode));

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

            CartProducts[newCartItem.CartProduct.Id] = newCartItem.CartProduct;
            await SetItemFulfillmentCenterAsync(lineItem, newCartItem.CartProduct);
            await UpdateVendor(lineItem, newCartItem.CartProduct);
            await InnerAddLineItemAsync(lineItem, newCartItem.OverrideQuantity, newCartItem.CartProduct, newCartItem.DynamicProperties);

            return this;
        }

        public virtual async Task<CartAggregate> AddItemsAsync(ICollection<NewCartItem> newCartItems)
        {
            EnsureCartExists();

            var productIds = newCartItems.Select(x => x.ProductId).Distinct().ToArray();
            var productsByIds = (await _cartProductService.GetCartProductsByIdsAsync(this, productIds)).ToDictionary(x => x.Id);

            foreach (var item in newCartItems)
            {
                if (!productsByIds.TryGetValue(item.ProductId, out var product))
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

            var productIds = giftRewards.Select(x => x.ProductId).Distinct().Where(x => !x.IsNullOrEmpty()).ToArray();
            if (productIds.Length == 0)
            {
                return new List<GiftItem>();
            }

            var productsByIds = (await _cartProductService.GetCartProductsByIdsAsync(this, productIds)).ToDictionary(x => x.Id);

            var availableProductsIds = productsByIds.Values
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
                    if (!reward.ProductId.IsNullOrEmpty() && productsByIds.TryGetValue(reward.ProductId, out var product))
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

            await RemoveExistingShipmentAsync(shipment);

            shipment.Currency = Cart.Currency;
            if (shipment.DeliveryAddress != null)
            {
                //Reset address key because it can equal a customer address from profile and if not do that it may cause
                //address primary key duplication error for multiple carts with the same address
                shipment.DeliveryAddress.Key = null;
            }
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
                await InnerAddLineItemAsync(lineItem, overrideQuantity: false, product: otherCart.CartProducts[lineItem.ProductId]);
            }
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

        public virtual async Task<IList<ValidationFailure>> ValidateAsync(CartValidationContext validationContext, string ruleSet)
        {
            ArgumentNullException.ThrowIfNull(validationContext);

            validationContext.CartAggregate = this;

            EnsureCartExists();
            var rules = ruleSet?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = await AbstractTypeFactory<CartValidator>.TryCreateInstance().ValidateAsync(validationContext, options => options.IncludeRuleSets(rules));
            if (!result.IsValid)
            {
                CartValidationErrors = result.Errors;
            }
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

            var validCoupon = promotionResult.Rewards.FirstOrDefault(x => x.IsValid && x.Coupon == coupon);

            return validCoupon != null;
        }

        public virtual async Task<PromotionResult> EvaluatePromotionsAsync()
        {
            EnsureCartExists();

            var promotionResult = new PromotionResult();
            if (!LineItems.IsNullOrEmpty() && !LineItems.Any(i => i.IsReadOnly))
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
            var taxProvider = await GetActiveTaxProviderAsync();
            if (taxProvider != null)
            {
                var taxEvalContext = _mapper.Map<TaxEvaluationContext>(this);
                result = taxProvider.CalculateRates(taxEvalContext);
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
            var existingLineItem = FindExistingLineItemBeforeAdd(newLineItem.ProductId, product, dynamicProperties);

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
            var existingLineItem = FindExistingLineItemBeforeAdd(newLineItem.ProductId, product, dynamicProperties);

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
        /// <param name="newDynamicProperties">new dynamuc properties that should be added/updated in cart line item</param>
        /// <returns></returns>
        protected virtual LineItem FindExistingLineItemBeforeAdd(string newProductId, CartProduct newProduct, IList<DynamicPropertyValue> newDynamicProperties)
        {
            return LineItems.FirstOrDefault(x => x.ProductId == newProductId);
        }

        protected virtual void EnsureCartExists()
        {
            if (Cart == null)
            {
                throw new OperationCanceledException("Cart not loaded.");
            }
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

        public virtual async Task<CartAggregate> AddConfigurationItemsAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections)
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

            return await AddConfigurationItemsAsync(lineItem, configurationSections);
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

            var productIds = configurationSections
                .Where(x => x.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation && !string.IsNullOrEmpty(x.Option?.ProductId))
                .Select(x => x.Option.ProductId)
                .Distinct()
                .ToArray();

            var products = productIds.Length > 0
                ? (await _cartProductService.GetCartProductsByIdsAsync(this, productIds)).ToDictionary(x => x.Product.Id)
                : null;

            foreach (var section in configurationSections)
            {
                await ApplyConfigurationSectionAsync(cloneItem, section, products);
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

        public virtual async Task<CartAggregate> UpdateConfigurationItemsAsync(string lineItemId, IList<ProductConfigurationSection> configurationSections)
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

            return await UpdateConfigurationItemsAsync(lineItem, configurationSections);
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

            // Load products for Product/Variation types (needed for both update and create)
            var productIds = configurationSections
                .Where(x => x.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation && !string.IsNullOrEmpty(x.Option?.ProductId))
                .Select(x => x.Option.ProductId)
                .Distinct()
                .ToArray();

            var products = productIds.Length > 0
                ? (await _cartProductService.GetCartProductsByIdsAsync(this, productIds)).ToDictionary(x => x.Product.Id)
                : null;

            var fileUrlsToDelete = new List<string>();

            // Update or create configuration items
            foreach (var section in configurationSections)
            {
                await ApplyConfigurationSectionAsync(cloneItem, section, products, fileUrlsToDelete);
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

        protected virtual async Task ApplyConfigurationSectionAsync(
            LineItem lineItem,
            ProductConfigurationSection section,
            Dictionary<string, CartProduct> products,
            IList<string> fileUrlsToDelete = null)
        {
            switch (section.Type)
            {
                case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation:
                {
                    if (products?.TryGetValue(section.Option.ProductId, out var cartProduct) != true)
                    {
                        OperationValidationErrors.Add(CartErrorDescriber.ProductUnavailableError(nameof(CatalogProduct), section.Option.ProductId));
                        return;
                    }

                    var configurationItem = GetOrCreateConfigurationItem(lineItem, section);
                    UpdateConfigurationItemForProduct(configurationItem, section, cartProduct);

                    break;
                }

                case ConfigurationSectionTypeText:
                {
                    var configurationItem = GetOrCreateConfigurationItem(lineItem, section);
                    UpdateConfigurationItemForText(configurationItem, section);

                    break;
                }

                case ConfigurationSectionTypeFile:
                {
                    var configurationItem = GetOrCreateConfigurationItem(lineItem, section);
                    if (fileUrlsToDelete != null && !configurationItem.Files.IsNullOrEmpty())
                    {
                        foreach (var url in configurationItem.Files.Select(x => x.Url).Except(section.FileUrls))
                        {
                            fileUrlsToDelete.Add(url);
                        }
                    }

                    await UpdateConfigurationItemForFilesAsync(configurationItem, section);

                    break;
                }
            }
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
            foreach (var section in configurationSections)
            {
                var configurationItem = FindConfigurationItem(cloneItem, section);
                if (configurationItem is null)
                {
                    // Already removed - no error (idempotent delete)
                    continue;
                }

                cloneItem.ConfigurationItems.Remove(configurationItem);

                // Collect file URLs for deferred deletion
                if (configurationItem.Type == ConfigurationSectionTypeFile && !configurationItem.Files.IsNullOrEmpty())
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
            foreach (var section in configurationSections)
            {
                switch (section.Type)
                {
                    case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation:
                        if (string.IsNullOrEmpty(section.Option?.ProductId))
                        {
                            OperationValidationErrors.Add(CartErrorDescriber.SelectedProductIsRequired(lineItem));
                        }
                        break;

                    case ConfigurationSectionTypeText:
                    case ConfigurationSectionTypeFile:
                        break;

                    default:
                        OperationValidationErrors.Add(CartErrorDescriber.ConfigurationSectionUnknownType(lineItem, section.Type, section.SectionId));
                        break;
                }
            }
        }

        protected virtual ConfigurationItem GetOrCreateConfigurationItem(LineItem lineItem, ProductConfigurationSection section)
        {
            var configurationItem = FindConfigurationItem(lineItem, section);
            if (configurationItem is null)
            {
                configurationItem = CreateConfigurationItem(section);
                lineItem.ConfigurationItems ??= new List<ConfigurationItem>();
                lineItem.ConfigurationItems.Add(configurationItem);
            }

            return configurationItem;
        }

        protected virtual ConfigurationItem FindConfigurationItem(LineItem lineItem, ProductConfigurationSection section)
        {
            return section.Type == ConfigurationSectionTypeVariation
                // For Variation: search by Type + SectionId + ProductId (multiple variations can exist)
                ? lineItem.ConfigurationItems?.FirstOrDefault(x => x.Type == section.Type && x.SectionId == section.SectionId && x.ProductId == section.Option?.ProductId)
                // For Product, Text and File: search only by Type + SectionId
                : lineItem.ConfigurationItems?.FirstOrDefault(x => x.Type == section.Type && x.SectionId == section.SectionId);
        }

        protected virtual ConfigurationItem CreateConfigurationItem(ProductConfigurationSection section)
        {
            var item = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();
            item.SectionId = section.SectionId;
            item.Type = section.Type;

            return item;
        }

        protected virtual void UpdateConfigurationItemForProduct(ConfigurationItem item, ProductConfigurationSection section, CartProduct cartProduct)
        {
            item.ProductId = section.Option.ProductId;
            item.Quantity = section.Option.Quantity;
            item.SelectedForCheckout = section.Option.SelectedForCheckout;
            item.Name = cartProduct.GetName(Cart.LanguageCode);
            item.Sku = cartProduct.Product.Code;
            item.ImageUrl = cartProduct.Product.ImgSrc;
            item.CatalogId = cartProduct.Product.CatalogId;
            item.CategoryId = cartProduct.Product.CategoryId;

            if (cartProduct.Price != null)
            {
                var tierPrice = cartProduct.Price.GetTierPrice(section.Option.Quantity);
                item.ListPrice = tierPrice.Price.Amount;
                item.SalePrice = tierPrice.ActualPrice.Amount;
            }
        }

        protected virtual void UpdateConfigurationItemForText(ConfigurationItem item, ProductConfigurationSection section)
        {
            item.CustomText = section.CustomText;
        }

        protected virtual async Task UpdateConfigurationItemForFilesAsync(ConfigurationItem item, ProductConfigurationSection section)
        {
            item.Files = await GetConfigurationFilesAsync(section.FileUrls);
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
            var configProductsIds = configuredItems
                .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                .SelectMany(x => x.ConfigurationItems.Where(c => c.ProductId != null).Select(c => c.ProductId))
                .Distinct()
                .ToArray();

            var configProducts = configProductsIds.Length > 0
                ? (await _cartProductService.GetCartProductsByIdsAsync(this, configProductsIds)).ToDictionary(x => x.Id)
                : new Dictionary<string, CartProduct>();

            foreach (var configurationLineItem in configuredItems)
            {
                var container = CreateConfiguredLineItemContainer(configurationLineItem, configProducts);
                container.UpdatePrice(configurationLineItem);
                container.SyncConfigurationPrices(configurationLineItem);
            }

            return this;
        }

        protected virtual ConfiguredLineItemContainer CreateConfiguredLineItemContainer(LineItem configurationLineItem, Dictionary<string, CartProduct> configProducts)
        {
            var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
            container.Currency = Currency;
            container.Store = Store;

            if (CartProducts.TryGetValue(configurationLineItem.ProductId, out var configurableProduct))
            {
                container.ConfigurableProduct = configurableProduct;
            }

            foreach (var configurationItem in configurationLineItem.ConfigurationItems ?? [])
            {
                switch (configurationItem.Type)
                {
                    case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation:
                    {
                        if (configProducts.TryGetValue(configurationItem.ProductId, out var product))
                        {
                            container.AddProductSectionLineItem(product, configurationItem);
                        }

                        break;
                    }
                    case ConfigurationSectionTypeText:
                        container.AddTextSectionLineItem(configurationItem.CustomText, configurationItem.SectionId);
                        break;
                    case ConfigurationSectionTypeFile:
                        container.AddFileSectionLineItem(configurationItem.Files, configurationItem.SectionId);
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
            result.CartProducts = CartProducts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.CloneTyped());
            result.Currency = Currency.CloneTyped();
            result.Member = Member?.CloneTyped();
            result.Store = Store.CloneTyped();

            return result;
        }

        #endregion ICloneable
    }
}
