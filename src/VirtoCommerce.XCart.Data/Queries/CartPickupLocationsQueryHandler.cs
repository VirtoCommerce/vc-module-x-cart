using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XPickup.Core.Models;
using VirtoCommerce.XPickup.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries;

public class CartPickupLocationsQueryHandler(
    IProductPickupLocationService productPickupLocationService,
    IShoppingCartService shoppingCartService)
    : IQueryHandler<CartPickupLocationsQuery, ProductPickupLocationSearchResult>
{
    public async Task<ProductPickupLocationSearchResult> Handle(CartPickupLocationsQuery request, CancellationToken cancellationToken)
    {
        var cart = await shoppingCartService.GetNoCloneAsync(request.CartId);
        if (cart == null)
        {
            throw new InvalidOperationException($"Cart with id {request.CartId} not found");
        }

        var searchCriteria = AbstractTypeFactory<MultipleProductsPickupLocationSearchCriteria>.TryCreateInstance();

        searchCriteria.StoreId = request.StoreId;
        searchCriteria.Products = cart.Items
            .Select(x => new ProductPickupLocationSearchCriteriaItem { ProductId = x.ProductId, Quantity = x.Quantity })
            .ToDictionary(x => x.ProductId);

        searchCriteria.Keyword = request.Keyword;
        searchCriteria.LanguageCode = request.CultureName;

        searchCriteria.Sort = request.Sort;
        searchCriteria.Skip = request.Skip;
        searchCriteria.Take = request.Take;

        return await productPickupLocationService.SearchPickupLocationsAsync(searchCriteria);
    }
}
