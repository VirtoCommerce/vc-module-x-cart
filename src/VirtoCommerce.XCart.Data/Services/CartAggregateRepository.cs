using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using static VirtoCommerce.Xapi.Core.ModuleConstants;
using CartAggregateBuilder = VirtoCommerce.Xapi.Core.Infrastructure.AsyncObjectBuilder<VirtoCommerce.XCart.Core.CartAggregate>;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartAggregateRepository : ICartAggregateRepository
    {
        private readonly Func<CartAggregate> _cartAggregateFactory;
        private readonly ICartProductService _cartProductsService;
        private readonly IShoppingCartSearchService _shoppingCartSearchService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly IMemberResolver _memberResolver;
        private readonly IStoreService _storeService;
        private readonly IFileUploadService _fileUploadService;

        private readonly IPlatformMemoryCache _platformMemoryCache;

        public CartAggregateRepository(
            Func<CartAggregate> cartAggregateFactory,
            IShoppingCartSearchService shoppingCartSearchService,
            IShoppingCartService shoppingCartService,
            ICurrencyService currencyService,
            IMemberResolver memberResolver,
            IStoreService storeService,
            ICartProductService cartProductsService,
            IPlatformMemoryCache platformMemoryCache,
            IFileUploadService fileUploadService)
        {
            _cartAggregateFactory = cartAggregateFactory;
            _shoppingCartSearchService = shoppingCartSearchService;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _memberResolver = memberResolver;
            _storeService = storeService;
            _cartProductsService = cartProductsService;
            _platformMemoryCache = platformMemoryCache;
            _fileUploadService = fileUploadService;
        }

        public virtual async Task SaveAsync(CartAggregate cartAggregate)
        {
            await cartAggregate.RecalculateAsync();
            cartAggregate.Cart.ModifiedDate = DateTime.UtcNow;

            await _shoppingCartService.SaveChangesAsync([cartAggregate.Cart]);

            await UpdateConfigurationFiles(cartAggregate.Cart);

            // Clear cache
            GenericCachingRegion<CartAggregate>.ExpireTokenForKey(cartAggregate.Id);
        }

        public async Task<CartAggregate> GetCartByIdAsync(string cartId, string cultureName = null)
        {
            return await GetCartByIdAsync(cartId, null, cultureName);
        }

        public async Task<CartAggregate> GetCartByIdAsync(string cartId, IList<string> productsIncludeFields, string cultureName = null)
        {
            return await GetCartByIdAsync(cartId, null, productsIncludeFields, cultureName);
        }

        public async Task<CartAggregate> GetCartByIdAsync(string cartId, string responseGroup, IList<string> productsIncludeFields, string cultureName = null)
        {
            if (CartAggregateBuilder.IsBuilding(out var cartAggregate))
            {
                return cartAggregate;
            }

            var cart = await _shoppingCartService.GetByIdAsync(cartId, responseGroup);
            if (cart != null)
            {
                return await InnerGetCartAggregateFromCartAsync(cart, cultureName ?? Language.InvariantLanguage.CultureName, productsIncludeFields, CartResponseGroup.Full.ToString());
            }
            return null;
        }

        public Task<CartAggregate> GetCartForShoppingCartAsync(ShoppingCart cart, string cultureName = null)
        {
            if (CartAggregateBuilder.IsBuilding(out var cartAggregate))
            {
                return Task.FromResult(cartAggregate);
            }

            return InnerGetCartAggregateFromCartAsync(cart, cultureName ?? Language.InvariantLanguage.CultureName, CartResponseGroup.Full.ToString());
        }

        public async Task<CartAggregate> GetCartAsync(ICartRequest cartRequest, string responseGroup = null)
        {
            if (CartAggregateBuilder.IsBuilding(out var cartAggregate))
            {
                return cartAggregate;
            }

            var criteria = new ShoppingCartSearchCriteria
            {
                StoreId = cartRequest.StoreId,
                // IMPORTANT! Need to specify customerId, otherwise any user cart could be returned while we expect anonymous in this case.
                CustomerId = cartRequest.UserId ?? AnonymousUser.UserName,
                OrganizationId = cartRequest.OrganizationId,
                Name = cartRequest.CartName,
                Currency = cartRequest.CurrencyCode,
                Type = cartRequest.CartType,
                ResponseGroup = EnumUtility.SafeParseFlags(responseGroup, CartResponseGroup.Full).ToString()
            };

            var cartSearchResult = await _shoppingCartSearchService.SearchAsync(criteria);
            //The null value for the Type parameter should be interpreted as a valuable parameter, and we must return a cart object with Type property that has null exactly set.
            //otherwise, for the case where the system contains carts with different Types, the resulting cart may be a random result.
            var cart = cartSearchResult.Results.FirstOrDefault(x => cartRequest.CartType != null || x.Type == null);
            if (cart != null)
            {
                return await InnerGetCartAggregateFromCartAsync(cart.Clone() as ShoppingCart, cartRequest.CultureName, criteria.ResponseGroup);
            }

            return null;
        }

        public async Task<CartAggregate> GetCartAsync(ShoppingCartSearchCriteria criteria, string cultureName)
        {
            if (CartAggregateBuilder.IsBuilding(out var cartAggregate))
            {
                return cartAggregate;
            }

            criteria = criteria.CloneTyped();
            criteria.CustomerId ??= AnonymousUser.UserName;

            var cartSearchResult = await _shoppingCartSearchService.SearchAsync(criteria);
            //The null value for the Type parameter should be interpreted as a valuable parameter, and we must return a cart object with Type property that has null exactly set.
            //otherwise, for the case where the system contains carts with different Types, the resulting cart may be a random result.
            var cart = cartSearchResult.Results.FirstOrDefault(x => criteria.Type != null || x.Type == null);
            if (cart != null)
            {
                return await InnerGetCartAggregateFromCartAsync(cart.Clone() as ShoppingCart, cultureName ?? Language.InvariantLanguage.CultureName, criteria.ResponseGroup);
            }

            return null;
        }

        public async Task<SearchCartResponse> SearchCartAsync(ShoppingCartSearchCriteria criteria)
        {
            return await SearchCartAsync(criteria, null);
        }

        public async Task<SearchCartResponse> SearchCartAsync(ShoppingCartSearchCriteria criteria, IList<string> productsIncludeFields)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            var searchResult = await _shoppingCartSearchService.SearchAsync(criteria);
            var cartAggregates = await GetCartsForShoppingCartsAsync(criteria, searchResult.Results, productsIncludeFields);

            return new SearchCartResponse { Results = cartAggregates, TotalCount = searchResult.TotalCount };
        }

        public virtual async Task RemoveCartAsync(string cartId)
        {
            await _shoppingCartService.DeleteAsync(new[] { cartId }, softDelete: true);
            GenericCachingRegion<CartAggregate>.ExpireTokenForKey(cartId);
        }

        protected virtual async Task<IList<CartAggregate>> GetCartsForShoppingCartsAsync(ShoppingCartSearchCriteria criteria, IList<ShoppingCart> carts, IList<string> productsIncludeFields, string cultureName = null)
        {
            var result = new List<CartAggregate>();

            foreach (var shoppingCart in carts)
            {
                result.Add(await InnerGetCartAggregateFromCartAsync(shoppingCart, cultureName ?? Language.InvariantLanguage.CultureName, productsIncludeFields, criteria.ResponseGroup));
            }

            return result;
        }

        protected virtual async Task<CartAggregate> InnerGetCartAggregateFromCartAsync(ShoppingCart cart, string language, string responseGroup)
        {
            return await InnerGetCartAggregateFromCartAsync(cart, language, null, responseGroup);
        }

        protected virtual async Task<CartAggregate> InnerGetCartAggregateFromCartAsync(ShoppingCart cart, string language, IList<string> productsIncludeFields, string responseGroup)
        {
            if (string.IsNullOrEmpty(cart.Id))
            {
                return await InnerGetCartAggregateFromCartNoCacheAsync(cart, language, productsIncludeFields, responseGroup);
            }

            var cacheKey = CacheKey.With(GetType(),
                nameof(InnerGetCartAggregateFromCartAsync),
                cart.Id,
                language,
                responseGroup ?? string.Empty,
                !productsIncludeFields.IsNullOrEmpty() ? string.Join(',', productsIncludeFields) : string.Empty);

            var result = await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async cacheOptions =>
            {
                cacheOptions.AddExpirationToken(GenericCachingRegion<CartAggregate>.CreateChangeTokenForKey(cart.Id));
                return await InnerGetCartAggregateFromCartNoCacheAsync(cart, language, productsIncludeFields, responseGroup);
            });

            return result;
        }

        private async Task<CartAggregate> InnerGetCartAggregateFromCartNoCacheAsync(ShoppingCart cart, string language, IList<string> productsIncludeFields, string responseGroup)
        {
            ArgumentNullException.ThrowIfNull(cart);

            var storeLoadTask = _storeService.GetByIdAsync(cart.StoreId);
            var allCurrenciesLoadTask = _currencyService.GetAllCurrenciesAsync();

            await Task.WhenAll(storeLoadTask, allCurrenciesLoadTask);

            var store = storeLoadTask.Result;
            var allCurrencies = allCurrenciesLoadTask.Result;

            if (store == null)
            {
                throw new OperationCanceledException($"store with id {cart.StoreId} not found");
            }

            // Set Default Currency
            if (string.IsNullOrEmpty(cart.Currency))
            {
                cart.Currency = store.DefaultCurrency;
            }

            // Actualize Cart Language From Context
            if (!string.IsNullOrEmpty(language) && cart.LanguageCode != language)
            {
                cart.LanguageCode = language;
            }

            language = !string.IsNullOrEmpty(cart.LanguageCode) ? cart.LanguageCode : store.DefaultLanguage;
            var currency = allCurrencies.GetCurrencyForLanguage(cart.Currency, language);

            var member = await _memberResolver.ResolveMemberByIdAsync(cart.CustomerId);
            var aggregate = _cartAggregateFactory();

            using (CartAggregateBuilder.Build(aggregate))
            {
                aggregate.GrabCart(cart, store, member, currency);

                //Load cart products explicitly if no validation is requested
                aggregate.ProductsIncludeFields = productsIncludeFields;
                aggregate.ResponseGroup = responseGroup;
                var cartProducts = await _cartProductsService.GetCartProductsByIdsAsync(aggregate, aggregate.Cart.Items.Select(x => x.ProductId).ToArray());
                //Populate aggregate.CartProducts with the  products data for all cart  line items
                foreach (var cartProduct in cartProducts)
                {
                    aggregate.CartProducts[cartProduct.Id] = cartProduct;
                }

                var validator = AbstractTypeFactory<CartLineItemPriceChangedValidator>.TryCreateInstance();
                foreach (var lineItem in aggregate.LineItems)
                {
                    var cartProduct = aggregate.CartProducts[lineItem.ProductId];
                    if (cartProduct == null)
                    {
                        continue;
                    }

                    await aggregate.SetItemFulfillmentCenterAsync(lineItem, cartProduct);
                    await aggregate.UpdateVendor(lineItem, cartProduct);

                    // validate price change
                    var lineItemContext = new CartLineItemPriceChangedValidationContext
                    {
                        LineItem = lineItem,
                        CartProducts = aggregate.CartProducts,
                    };

                    var result = await validator.ValidateAsync(lineItemContext);
                    if (!result.IsValid)
                    {
                        aggregate.ValidationWarnings.AddRange(result.Errors);
                    }

                    // update price for non-configured line items immediately 
                    if (!lineItem.IsConfigured)
                    {
                        aggregate.SetLineItemTierPrice(cartProduct.Price, lineItem.Quantity, lineItem);
                    }
                }

                await UpdateConfiguredLineItemPrice(aggregate);

                await aggregate.RecalculateAsync();

                return aggregate;
            }
        }

        private static async Task UpdateConfiguredLineItemPrice(CartAggregate aggregate)
        {
            var configurationLineItems = aggregate.LineItems.Where(x => x.IsConfigured).ToArray();
            await aggregate.UpdateConfiguredLineItemPrice(configurationLineItems);
        }

        private async Task UpdateConfigurationFiles(ShoppingCart cart)
        {
            var configurationItems = cart.Items.Where(x => !x.ConfigurationItems.IsNullOrEmpty()).SelectMany(x => x.ConfigurationItems.Where(y => y.Files != null));
            var fileUrls = configurationItems
                .SelectMany(y => y.Files)
                .Where(x => !string.IsNullOrEmpty(x.Url)).Select(x => x.Url)
                .Distinct().ToArray();

            var ids = fileUrls
                .Select(FileExtensions.GetFileId)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var files = await _fileUploadService.GetAsync(ids);

            files = files
                .Where(x => x.Scope == CatalogModule.Core.ModuleConstants.ConfigurationSectionFilesScope && string.IsNullOrEmpty(x.OwnerEntityId) && string.IsNullOrEmpty(x.OwnerEntityType))
                .ToList();

            if (!files.IsNullOrEmpty())
            {
                foreach (var file in files)
                {
                    var configurationItem = configurationItems.FirstOrDefault(x => x.Files.Any(y => y.Url == FileExtensions.GetFileUrl(file.Id)));
                    file.OwnerEntityId = configurationItem?.Id;
                    file.OwnerEntityType = nameof(ConfigurationItem);
                }

                await _fileUploadService.SaveChangesAsync(files);
            }
        }
    }
}
