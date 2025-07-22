using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class SavedForLaterListService(ICartAggregateRepository cartAggregateRepository) : ISavedForLaterListService
{
    protected const string savedForLaterCartType = ModuleConstants.ListTypeName;
    protected const string savedForLaterCartName = "Saved for later";

    public virtual async Task<CartAggregateWithList> MoveFromSavedForLaterItems(MoveSavedForLaterItemsCommandBase request)
    {
        var cart = await cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);

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

        return new CartAggregateWithList() { Cart = cart, List = savedForLaterList };
    }

    public virtual async Task<CartAggregateWithList> MoveToSavedForLaterItems(MoveSavedForLaterItemsCommandBase request)
    {
        var cart = await cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);

        if (cart == null)
        {
            throw new OperationCanceledException("Cart not found");
        }


        var savedForLaterList = await EnsureSaveForLaterListAsync(request);

        await MoveItemsAsync(cart, savedForLaterList, request.LineItemIds);

        return new CartAggregateWithList() { Cart = cart, List = savedForLaterList };
    }

    public virtual async Task<CartAggregate> FindSavedForLaterListAsync(ICartRequest request)
    {
        var searchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

        searchCriteria.Type = savedForLaterCartType;
        searchCriteria.Name = savedForLaterCartName;
        searchCriteria.StoreId = request.StoreId;
        searchCriteria.CustomerId = request.UserId;
        searchCriteria.LanguageCode = request.CultureName;
        searchCriteria.OrganizationId = request.OrganizationId;
        searchCriteria.Currency = request.CurrencyCode;

        return await cartAggregateRepository.GetCartAsync(searchCriteria, request.CultureName);
    }

    protected virtual async Task<CartAggregate> CreateSaveForLaterListAsync(ICartRequest request)
    {
        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();

        cart.Type = savedForLaterCartType;
        cart.Name = savedForLaterCartName;
        cart.StoreId = request.StoreId;
        cart.CustomerId = request.UserId;
        cart.LanguageCode = request.CultureName;
        cart.OrganizationId = request.OrganizationId;
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
        foreach (var lineItemId in lineItemIds)
        {
            var item = from.Cart.Items.FirstOrDefault(x => x.Id == lineItemId);

            if (item != null)
            {
                await to.AddItemsAsync(new List<NewCartItem> { new NewCartItem(item.ProductId, item.Quantity) });
                await from.RemoveItemAsync(lineItemId);
            }
        }

        await cartAggregateRepository.SaveAsync(from);
        await cartAggregateRepository.SaveAsync(to);
    }
}
