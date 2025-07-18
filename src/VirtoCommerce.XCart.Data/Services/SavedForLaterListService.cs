using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class SavedForLaterListService(ICartAggregateRepository cartAggregateRepository) : ISavedForLaterListService
{
    protected const string savedForLaterCartType = "SavedForLater";//TODO: use name too

    public virtual async Task<CartAggregate> EnsureSaveForLaterListAsync(ICartRequest request)
    {
        var existingSaveForLaterList = await FindSavedForLaterListAsync(request);

        if (existingSaveForLaterList != null)
        {
            return existingSaveForLaterList;
        }

        return await CreateSaveForLaterListAsync(request);
    }

    public virtual async Task<CartAggregate> FindSavedForLaterListAsync(ICartRequest request)
    {
        var searchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();
        searchCriteria.Type = savedForLaterCartType;
        searchCriteria.StoreId = request.StoreId;
        searchCriteria.CustomerId = request.UserId;
        searchCriteria.OrganizationId = request.OrganizationId;
        searchCriteria.Currency = request.CurrencyCode;

        return await cartAggregateRepository.GetCartAsync(searchCriteria, request.CultureName);
    }

    public virtual async Task<CartAggregate> CreateSaveForLaterListAsync(ICartRequest request)
    {
        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();

        cart.CustomerId = request.UserId;
        cart.OrganizationId = request.OrganizationId;
        cart.Name = request.CartName ?? "default";
        cart.StoreId = request.StoreId;
        cart.LanguageCode = request.CultureName;
        cart.Currency = request.CurrencyCode;
        cart.Items = new List<LineItem>();
        cart.Type = request.CartType;
        cart.Shipments = new List<Shipment>();
        cart.Payments = new List<Payment>();
        cart.Addresses = new List<CartModule.Core.Model.Address>();
        cart.TaxDetails = new List<TaxDetail>();
        cart.Coupons = new List<string>();
        cart.Discounts = new List<Discount>();
        cart.DynamicProperties = new List<DynamicObjectProperty>();

        return await cartAggregateRepository.GetCartForShoppingCartAsync(cart);
    }
}
