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
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Services;
using XCatalogConstants = VirtoCommerce.XCatalog.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Queries;

public class CartPickupLocationsQueryHandler(
    ICatalogPickupLocationService catalogPickupLocationService,
    IShoppingCartService shoppingCartService,
    IItemService itemService,
    IStoreService storeService) : IQueryHandler<CartPickupLocationsQuery, ProductPickupLocationSearchResult>
{
    public async Task<ProductPickupLocationSearchResult> Handle(CartPickupLocationsQuery request, CancellationToken cancellationToken)
    {
        var store = await storeService.GetNoCloneAsync(request.StoreId);
        if (store == null)
        {
            throw new InvalidOperationException($"Store with id {request.StoreId} not found");
        }

        var cart = await shoppingCartService.GetNoCloneAsync(request.CartId);
        if (cart == null)
        {
            throw new InvalidOperationException($"Cart with id {request.CartId} not found");
        }

        var result = AbstractTypeFactory<ProductPickupLocationSearchResult>.TryCreateInstance();

        if (!await catalogPickupLocationService.IsPickupInStoreEnabledAsync(request.StoreId))
        {
            return result;
        }

        var globalTransferEnabled = catalogPickupLocationService.GlobalTransferEnabled(store);

        var quantityByProductId = cart.Items.Select(x => new { x.ProductId, x.Quantity }).ToDictionary(x => x.ProductId);
        var productIds = quantityByProductId.Keys.ToList();

        var products = await itemService.GetByIdsAsync(productIds, responseGroup: null, catalogId: null);

        var pickupLocations = await catalogPickupLocationService.SearchProductPickupLocationsAsync(request.StoreId, request.Keyword);

        var productInventories = await catalogPickupLocationService.SearchProductInventoriesAsync(productIds);

        var resultItems = new List<ProductPickupLocation>();

        var worstAvailability = store.Settings.GetValue<bool>(XCatalogConstants.Settings.GlobalTransferEnabled) ? CartPickupAvailability.GlobalTransfer : CartPickupAvailability.Transfer;

        foreach (var pickupLocation in pickupLocations)
        {
            var worstProductAvailability = default(string);

            foreach (var product in products)
            {
                var pickupLocationProductInventories = productInventories
                    .Where(x => x.ProductId == product.Id)
                    .Where(x => x.FulfillmentCenterId == pickupLocation.FulfillmentCenterId || pickupLocation.TransferFulfillmentCenterIds.Contains(x.FulfillmentCenterId))
                    .ToList();

                var productAvailability = GetProductPickupLocationAvailability(store, product, pickupLocation, pickupLocationProductInventories, quantityByProductId[product.Id].Quantity, request.CultureName, globalTransferEnabled);

                if (worstProductAvailability == null)
                {
                    worstProductAvailability = productAvailability;
                }
                else
                {
                    worstProductAvailability = GetWorstAvailability(worstProductAvailability, productAvailability);
                }

                if (worstProductAvailability == worstAvailability)
                {
                    break;
                }
            }

            var productPickupLocation = await catalogPickupLocationService.CreatePickupLocationFromProductInventoryAsync(pickupLocation, productInventoryInfo: null, worstProductAvailability, request.CultureName);
            resultItems.Add(productPickupLocation);
        }

        result.TotalCount = resultItems.Count;
        result.Results = catalogPickupLocationService.ApplySort(resultItems, request.Sort).Skip(request.Skip).Take(request.Take).ToList();

        return result;
    }

    protected virtual string GetProductPickupLocationAvailability(Store store, CatalogProduct product, PickupLocation pickupLocation, IList<InventoryInfo> pickupLocationProductInventories, long minQuantity, string cultureName, bool globalTransferEnabled)
    {
        if (!product.TrackInventory.GetValueOrDefault())
        {
            return CartPickupAvailability.Today;
        }

        var mainPickupLocationProductInventory = catalogPickupLocationService.GetMainPickupLocationProductInventory(pickupLocation, pickupLocationProductInventories, minQuantity, order: true);
        if (mainPickupLocationProductInventory != null)
        {
            return CartPickupAvailability.Today;
        }

        var transferPickupLocationProductInventory = catalogPickupLocationService.GetTransferPickupLocationProductInventory(pickupLocation, pickupLocationProductInventories, minQuantity, order: true);
        if (transferPickupLocationProductInventory != null)
        {
            return CartPickupAvailability.Transfer;
        }

        if (globalTransferEnabled)
        {
            return CartPickupAvailability.GlobalTransfer;
        }

        return null;
    }

    protected string GetWorstAvailability(string productAvailability1, string productAvailability2)
    {
        if (productAvailability1 == productAvailability2)
        {
            return productAvailability1;
        }

        if (productAvailability1 == CartPickupAvailability.GlobalTransfer)
        {
            return productAvailability1;
        }
        if (productAvailability2 == CartPickupAvailability.GlobalTransfer)
        {
            return productAvailability2;
        }

        if (productAvailability1 == CartPickupAvailability.Transfer)
        {
            return productAvailability1;
        }
        if (productAvailability2 == CartPickupAvailability.Transfer)
        {
            return productAvailability2;
        }


        if (productAvailability1 == CartPickupAvailability.Today)
        {
            return productAvailability1;
        }
        if (productAvailability2 == CartPickupAvailability.Today)
        {
            return productAvailability2;
        }

        return productAvailability1;
    }
}
