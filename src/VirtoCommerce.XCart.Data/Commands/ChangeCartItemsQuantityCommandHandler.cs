using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartItemsQuantityCommandHandler : CartCommandHandler<ChangeCartItemsQuantityCommand>
    {
        readonly ICartProductService _cartProductService;

        public ChangeCartItemsQuantityCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartProductService cartProductService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
        }

        public async override Task<CartAggregate> Handle(ChangeCartItemsQuantityCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var requestItems = request.CartItems.ToList();
            foreach (var requestItem in request.CartItems.Where(x => x.Quantity == 0))
            {
                await cartAggregate.RemoveItemAsync(requestItem.LineItemId);
                requestItems.Remove(requestItem);
            }

            var quantityAdjustments = new List<ItemQtyAdjustment>();
            foreach (var requestItem in requestItems)
            {
                var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id.Equals(requestItem.LineItemId));
                if (lineItem != null)
                {
                    quantityAdjustments.Add(new ItemQtyAdjustment
                    {
                        LineItem = lineItem,
                        LineItemId = requestItem.LineItemId,
                        NewQuantity = requestItem.Quantity
                    });
                }
            }

            // load products for line items
            var productIds = quantityAdjustments.Select(x => x.LineItem.ProductId).ToArray();
            var productsByIds =
                (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, productIds))
                .ToDictionary(x => x.Id);

            foreach (var adjustment in quantityAdjustments)
            {
                if (productsByIds.TryGetValue(adjustment.LineItem.ProductId, out var product))
                {
                    adjustment.CartProduct = product;

                    await cartAggregate.ChangeItemQuantityAsync(adjustment);
                }
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
