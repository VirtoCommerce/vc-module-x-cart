using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPricesSumQueryHandler : IQueryHandler<GetPricesSumQuery, ExpPricesSum>
{
    private readonly ICartAggregateRepository _cartAggregateRepository;

    public GetPricesSumQueryHandler(ICartAggregateRepository cartAggregateRepository)
    {
        _cartAggregateRepository = cartAggregateRepository;
    }

    public async Task<ExpPricesSum> Handle(GetPricesSumQuery request, CancellationToken cancellationToken)
    {
        var result = new ExpPricesSum();

        if (string.IsNullOrEmpty(request.CartId) || request.LineItemIds.IsNullOrEmpty())
        {
            return result;
        }

        var currentCartAggregate = await _cartAggregateRepository.GetCartByIdAsync(request.CartId);
        var tempCartAggregate = await CreateNewCartAggregateAsync(request);

        await CopyItems(currentCartAggregate, tempCartAggregate, request.LineItemIds);
        await tempCartAggregate.RecalculateAsync();

        result.Currency = tempCartAggregate.Currency;
        result.Total = tempCartAggregate.Cart.Total;
        result.DiscountTotal = tempCartAggregate.Cart.DiscountAmount;

        return result;
    }

    protected virtual async Task CopyItems(CartAggregate currentCartAggregate, CartAggregate tempCartAggregate, IList<string> lineItemIds)
    {
        var items = currentCartAggregate.LineItems
            .Where(x => lineItemIds.Contains(x.Id))
            .ToArray();

        if (items.Length > 0)
        {
            var newCartItems = items
                .Select(x => new NewCartItem(x.ProductId, x.Quantity)
                {
                    IgnoreValidationErrors = true,
                })
                .ToArray();

            await tempCartAggregate.AddItemsAsync(newCartItems);
        }
    }

    protected virtual Task<CartAggregate> CreateNewCartAggregateAsync(GetPricesSumQuery request)
    {
        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();

        cart.Name = "default";
        cart.CustomerId = request.UserId;
        cart.OrganizationId = request.OrganizationId;
        cart.StoreId = request.StoreId;
        cart.LanguageCode = request.CultureName;
        cart.Currency = request.CurrencyCode;
        cart.Items = new List<LineItem>();
        cart.Discounts = new List<Discount>();

        return _cartAggregateRepository.GetCartForShoppingCartAsync(cart);
    }
}
