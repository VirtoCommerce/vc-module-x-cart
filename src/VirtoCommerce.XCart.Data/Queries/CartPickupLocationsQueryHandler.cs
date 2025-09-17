using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.InventoryModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using ShippingConstants = VirtoCommerce.ShippingModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Queries;

public class CartPickupLocationsQueryHandler(
    IShoppingCartService shoppingCartService,
    IItemService itemService,
    //IProductInventorySearchService productInventorySearchService,
    IShippingMethodsSearchService shippingMethodsSearchService,
    IPickupLocationSearchService pickupLocationSearchService,
    IStoreService storeService//,
    /*ILocalizableSettingService localizableSettingService*/) : IQueryHandler<CartPickupLocationsQuery, CartPickupLocationSearchResult>
{
    public async Task<CartPickupLocationSearchResult> Handle(CartPickupLocationsQuery request, CancellationToken cancellationToken)
    {
        var store = await storeService.GetNoCloneAsync(request.StoreId);
        if (store == null)
        {
            throw new InvalidOperationException($"Store with id {request.StoreId} not found");
        }

        var result = AbstractTypeFactory<CartPickupLocationSearchResult>.TryCreateInstance();

        if (await IsPickupInStoreEnabled(request))
        {
            var cart = await shoppingCartService.GetNoCloneAsync(request.CartId);
            if (cart == null)
            {
                throw new InvalidOperationException($"Cart with id {request.CartId} not found");
            }

            var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToList();

            var products = await itemService.GetByIdsAsync(productIds, responseGroup: null, catalogId: null);

            var pickupLocations = await SearchProductPickupLocations(request);

            var productInventories = await SearchProductInventoriesAsync(request);

            var resultItems = new List<CartPickupLocation>();

            foreach (var product in products)
            {
                foreach (var pickupLocation in pickupLocations)
                {
                    var pickupLocationProductInventories = productInventories
                        .Where(x => x.ProductId == product.Id)
                        .Where(x => x.FulfillmentCenterId == pickupLocation.FulfillmentCenterId || pickupLocation.TransferFulfillmentCenterIds.Contains(x.FulfillmentCenterId))
                        .ToList();

                    var productPickupLocation = await GetProductPickupLocationAsync(store, product, pickupLocation, pickupLocationProductInventories, request.CultureName);
                    if (productPickupLocation != null)
                    {
                        resultItems.Add(productPickupLocation);
                    }
                }
            }

            result.TotalCount = resultItems.Count;
            result.Results = ApplySort(resultItems, request).Skip(request.Skip).Take(request.Take).ToList();
        }

        return result;
    }

    protected virtual async Task<bool> IsPickupInStoreEnabled(CartPickupLocationsQuery request)
    {
        var shippingMethodsSearchCriteria = AbstractTypeFactory<ShippingMethodsSearchCriteria>.TryCreateInstance();
        shippingMethodsSearchCriteria.StoreId = request.StoreId;
        shippingMethodsSearchCriteria.IsActive = true;
        shippingMethodsSearchCriteria.Codes = [ShippingConstants.BuyOnlinePickupInStoreShipmentCode];
        shippingMethodsSearchCriteria.Skip = 0;
        shippingMethodsSearchCriteria.Take = 1;

        return (await shippingMethodsSearchService.SearchNoCloneAsync(shippingMethodsSearchCriteria)).TotalCount > 0;
    }

    protected virtual async Task<IList<PickupLocation>> SearchProductPickupLocations(CartPickupLocationsQuery request)
    {
        var pickupLocationSearchCriteria = AbstractTypeFactory<PickupLocationSearchCriteria>.TryCreateInstance();
        pickupLocationSearchCriteria.StoreId = request.StoreId;
        pickupLocationSearchCriteria.IsActive = true;
        pickupLocationSearchCriteria.Keyword = request.Keyword;
        pickupLocationSearchCriteria.Sort = request.Sort;

        return await pickupLocationSearchService.SearchAllNoCloneAsync(pickupLocationSearchCriteria);
    }

    protected virtual async Task<IList<InventoryInfo>> SearchProductInventoriesAsync(CartPickupLocationsQuery request)
    {
        return await Task.FromResult(new List<InventoryInfo>());

        //var productInventorySearchCriteria = AbstractTypeFactory<ProductInventorySearchCriteria>.TryCreateInstance();
        //productInventorySearchCriteria.ProductId = request.ProductId;//TODO: productIds

        //return await productInventorySearchService.SearchAllAsync(productInventorySearchCriteria, clone: false);
    }

    protected virtual async Task<CartPickupLocation> GetProductPickupLocationAsync(Store store, CatalogProduct product, PickupLocation pickupLocation, IList<InventoryInfo> pickupLocationProductInventories, string cultureName)
    {
        if (!product.TrackInventory.GetValueOrDefault())
        {
            return await CreatePickupLocationFromProductInventoryAsync(pickupLocation, CartPickupAvailability.Today, cultureName);
        }

        var mainPickupLocationProductInventory = pickupLocationProductInventories
            .Where(x => x.FulfillmentCenterId == pickupLocation.FulfillmentCenterId)
            .Where(x => x.InStockQuantity > 0)
            .OrderByDescending(x => x.InStockQuantity)
            .FirstOrDefault();

        if (mainPickupLocationProductInventory != null)
        {
            return await CreatePickupLocationFromProductInventoryAsync(pickupLocation, CartPickupAvailability.Today, cultureName);
        }

        var transferPickupLocationProductInventory = pickupLocationProductInventories
            .Where(x => pickupLocation.TransferFulfillmentCenterIds.Contains(x.FulfillmentCenterId))
            .Where(x => x.InStockQuantity > 0)
            .OrderByDescending(x => x.InStockQuantity)
            .FirstOrDefault();

        if (transferPickupLocationProductInventory != null)
        {
            return await CreatePickupLocationFromProductInventoryAsync(pickupLocation, CartPickupAvailability.Transfer, cultureName);
        }

        //if (store.Settings.GetValue<bool>(ModuleConstants.Settings.GlobalTransferEnabled))
        //{
        //    return await CreatePickupLocationFromProductInventoryAsync(pickupLocation, CartPickupAvailability.GlobalTransfer, cultureName);
        //}

        return null;
    }

    protected virtual async Task<CartPickupLocation> CreatePickupLocationFromProductInventoryAsync(PickupLocation pickupLocation, string productPickupAvailability, string cultureName)
    {
        var result = AbstractTypeFactory<CartPickupLocation>.TryCreateInstance();

        result.PickupLocation = new PickupLocation();
        result.PickupLocation.Id = pickupLocation.Id;
        result.PickupLocation.Name = pickupLocation.Name;
        result.PickupLocation.Address = pickupLocation.Address;
        result.PickupLocation.GeoLocation = pickupLocation.GeoLocation;
        result.AvailabilityType = productPickupAvailability;
        result.Note = await GetProductPickupLocationNoteAsync(productPickupAvailability, cultureName);

        return result;
    }

    protected virtual async Task<string> GetProductPickupLocationNoteAsync(string productPickupAvailability, string cultureName)
    {
        return await Task.FromResult(productPickupAvailability.ToString());

        //if (productPickupAvailability == CartPickupAvailability.Today)
        //{
        //    var result = (await localizableSettingService.GetValuesAsync(ModuleConstants.Settings.TodayAvailabilityNote.Name, cultureName)).FirstOrDefault()?.Value;
        //    if (string.IsNullOrEmpty(result))
        //    {
        //        result = "Today";
        //    }
        //    return result;
        //}
        //else if (productPickupAvailability == CartPickupAvailability.Transfer)
        //{
        //    var result = (await localizableSettingService.GetValuesAsync(ModuleConstants.Settings.TransferAvailabilityNote.Name, cultureName)).FirstOrDefault()?.Value;
        //    if (string.IsNullOrEmpty(result))
        //    {
        //        result = "Via transfer";
        //    }
        //    return result;
        //}
        //else if (productPickupAvailability == CartPickupAvailability.GlobalTransfer)
        //{
        //    var result = (await localizableSettingService.GetValuesAsync(ModuleConstants.Settings.GlobalTransferAvailabilityNote.Name, cultureName)).FirstOrDefault()?.Value;
        //    if (string.IsNullOrEmpty(result))
        //    {
        //        result = "Via transfer";
        //    }
        //    return result;
        //}

        //return null;
    }

    protected virtual IEnumerable<CartPickupLocation> ApplySort(IList<CartPickupLocation> items, CartPickupLocationsQuery request)
    {
        if (request.Sort.IsNullOrEmpty())
        {
            return items
                .OrderBy(x => GetAvaiabilitySortOrder(x.AvailabilityType))
                .ThenBy(x => x.PickupLocation.Name);
        }

        return items;
    }

    protected virtual int GetAvaiabilitySortOrder(string availabilityType)
    {
        return availabilityType switch
        {
            CartPickupAvailability.Today => 10,
            CartPickupAvailability.Transfer => 20,
            CartPickupAvailability.GlobalTransfer => 30,
            _ => 100
        };
    }
}
