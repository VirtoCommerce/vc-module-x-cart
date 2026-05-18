using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;

namespace VirtoCommerce.XCart.Data.Services;

public class SavedForLaterListService(
    ICartAggregateRepository cartAggregateRepository,
    ICartProductService cartProductService,
    IFileUploadService fileUploadService) : ISavedForLaterListService
{
    protected const string savedForLaterDefaultName = "Saved for later";

    public virtual async Task<CartAggregateWithList> MoveFromSavedForLaterItems(MoveSavedForLaterItemsCommandBase request)
    {
        var cart = request.Cart ?? await cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);

        if (cart == null)
        {
            throw new OperationCanceledException("Cart not found");
        }

        var savedForLaterList = await FindSavedForLaterListAsync(request);

        if (savedForLaterList == null)
        {
            throw new OperationCanceledException("Saved for later list not found");
        }

        await MoveItemsAsync(savedForLaterList, cart, request.LineItemIds);

        return new CartAggregateWithList { Cart = cart, List = savedForLaterList };
    }

    public virtual async Task<CartAggregateWithList> MoveToSavedForLaterItems(MoveSavedForLaterItemsCommandBase request)
    {
        var cart = request.Cart ?? await cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);

        if (cart == null)
        {
            throw new OperationCanceledException("Cart not found");
        }

        var savedForLaterList = await EnsureSaveForLaterListAsync(request);

        await MoveItemsAsync(cart, savedForLaterList, request.LineItemIds);

        return new CartAggregateWithList { Cart = cart, List = savedForLaterList };
    }

    public virtual async Task<CartAggregate> FindSavedForLaterListAsync(ICartRequest request)
    {
        var searchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

        searchCriteria.Type = CartType.SavedForLater;
        searchCriteria.StoreId = request.StoreId;
        searchCriteria.CustomerId = request.UserId;
        searchCriteria.LanguageCode = request.CultureName;
        searchCriteria.Currency = request.CurrencyCode;

        return await cartAggregateRepository.GetCartAsync(searchCriteria, request.CultureName);
    }

    protected virtual async Task<CartAggregate> CreateSaveForLaterListAsync(ICartRequest request)
    {
        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();

        cart.Type = CartType.SavedForLater;
        cart.Name = savedForLaterDefaultName;
        cart.StoreId = request.StoreId;
        cart.CustomerId = request.UserId;
        cart.LanguageCode = request.CultureName;
        cart.Currency = request.CurrencyCode;
        cart.Items = new List<LineItem>();
        cart.Shipments = new List<Shipment>();
        cart.Payments = new List<Payment>();
        cart.Addresses = new List<CartModule.Core.Model.Address>();
        cart.TaxDetails = new List<TaxDetail>();
        cart.Coupons = new List<string>();
        cart.Discounts = new List<Discount>();
        cart.DynamicProperties = new List<DynamicObjectProperty>();

        return await cartAggregateRepository.GetCartForShoppingCartAsync(cart);
    }

    protected async Task<CartAggregate> EnsureSaveForLaterListAsync(ICartRequest request)
    {
        var existingSaveForLaterList = await FindSavedForLaterListAsync(request);

        if (existingSaveForLaterList != null)
        {
            return existingSaveForLaterList;
        }

        return await CreateSaveForLaterListAsync(request);
    }

    protected async Task MoveItemsAsync(CartAggregate from, CartAggregate to, IList<string> lineItemIds)
    {
        var lineItemIdsToMove = lineItemIds.ToHashSet();
        var lineItemsToMove = from.Cart.Items.Where(x => lineItemIdsToMove.Contains(x.Id)).ToArray();

        if (lineItemsToMove.IsNullOrEmpty())
        {
            return;
        }

        to.ValidationRuleSet = ["default"];

        await CopyOrdinaryItemsAsync(lineItemsToMove, to);
        await CopyConfiguredItemsAsync(lineItemsToMove, from, to);

        foreach (var item in lineItemsToMove)
        {
            await from.RemoveItemAsync(item.Id);
        }

        await cartAggregateRepository.SaveAsync(from);
        await cartAggregateRepository.SaveAsync(to);
    }

    protected static async Task CopyOrdinaryItemsAsync(IList<LineItem> sourceItems, CartAggregate to)
    {
        var ordinaryItems = sourceItems.Where(x => !x.IsConfigured).ToArray();

        if (ordinaryItems.IsNullOrEmpty())
        {
            return;
        }

        var newCartItems = ordinaryItems.Select(BuildNewCartItem).ToArray();

        await to.AddItemsAsync(newCartItems);
    }

    protected async Task CopyConfiguredItemsAsync(IList<LineItem> sourceItems, CartAggregate from, CartAggregate to)
    {
        var configuredItems = sourceItems.Where(x => x.IsConfigured).ToArray();

        if (configuredItems.IsNullOrEmpty())
        {
            return;
        }

        var configuredProductIds = configuredItems
            .SelectMany(GetReferencedProductIds)
            .Where(id => !id.IsNullOrEmpty())
            .Distinct()
            .ToArray();

        var configuredProducts = await cartProductService.GetCartProductsByIdsAsync(to, configuredProductIds);
        var configuredProductDictionary = configuredProducts.ToDictionary(x => x.Id);

        foreach (var configuredItem in configuredItems)
        {
            var container = await CreateConfiguredLineItemContainerAsync(configuredItem, configuredProductDictionary, from, to);
            if (container is null)
            {
                continue;
            }

            var newConfiguredItem = container.CreateConfiguredLineItem(configuredItem.Quantity).Item;

            var newCartItem = BuildNewCartItem(configuredItem);
            newCartItem.CartProduct = container.ConfigurableProduct;

            await to.AddConfiguredItemAsync(newCartItem, newConfiguredItem);
        }

        static IEnumerable<string> GetReferencedProductIds(LineItem item)
        {
            yield return item.ProductId;
            foreach (var section in item.ConfigurationItems ?? [])
            {
                yield return section.ProductId;
            }
        }
    }

    protected virtual async Task<ConfiguredLineItemContainer> CreateConfiguredLineItemContainerAsync(
        LineItem configurationLineItem,
        Dictionary<string, CartProduct> configuredProductDictionary,
        CartAggregate from,
        CartAggregate to)
    {
        if (!configuredProductDictionary.TryGetValue(configurationLineItem.ProductId, out var configurableProduct))
        {
            return null;
        }

        var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
        container.Currency = to.Currency;
        container.Store = to.Store;
        container.ConfigurableProduct = configurableProduct;

        if (configurationLineItem.ConfigurationItems.IsNullOrEmpty())
        {
            return container;
        }

        foreach (var configurationItem in configurationLineItem.ConfigurationItems)
        {
            switch (configurationItem.Type)
            {
                case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation
                    when configuredProductDictionary.TryGetValue(configurationItem.ProductId, out var product):
                    container.AddProductSectionLineItem(product, configurationItem);
                    break;

                case ConfigurationSectionTypeText:
                    container.AddTextSectionLineItem(configurationItem);
                    break;

                case ConfigurationSectionTypeFile:
                    var configurationFiles = await CopyConfigurationFiles(configurationItem, from.Cart);
                    container.AddFileSectionLineItem(configurationItem, configurationFiles);
                    break;
            }
        }

        return container;
    }

    private static NewCartItem BuildNewCartItem(LineItem source)
    {
        return new NewCartItem(source.ProductId, source.Quantity)
        {
            IgnoreValidationErrors = true,
            CreatedDate = source.CreatedDate,
            Comment = source.Note,
            IsSelectedForCheckout = source.SelectedForCheckout,
            DynamicProperties = MapDynamicProperties(source.DynamicProperties),
        };
    }

    private async Task<IList<ConfigurationItemFile>> CopyConfigurationFiles(ConfigurationItem configurationItem, ShoppingCart cart)
    {
        if (configurationItem.Files.IsNullOrEmpty())
        {
            return [];
        }

        var fileUrls = configurationItem.Files
            .Select(x => x.Url)
            .Where(url => !url.IsNullOrWhiteSpace())
            .Distinct()
            .ToArray();

        if (fileUrls.IsNullOrEmpty())
        {
            return [];
        }

        var files = await fileUploadService.GetByPublicUrlAsync(fileUrls);

        return files.Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIs(cart))
           .Select(x => x.ConvertToConfigurationItemFile())
           .ToArray();
    }

    private static DynamicPropertyValue[] MapDynamicProperties(ICollection<DynamicObjectProperty> dynamicProperties)
    {
        if (dynamicProperties.IsNullOrEmpty())
        {
            return [];
        }

        return dynamicProperties
            .SelectMany(p => p.Values, (p, v) =>
            {
                var value = AbstractTypeFactory<DynamicPropertyValue>.TryCreateInstance();
                value.Name = p.Name;
                value.Value = v.Value;
                value.Locale = v.Locale;
                return value;
            })
            .ToArray();
    }
}
