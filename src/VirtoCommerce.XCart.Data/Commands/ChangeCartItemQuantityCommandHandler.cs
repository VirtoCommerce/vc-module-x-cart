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
    public class ChangeCartItemQuantityCommandHandler : CartCommandHandler<ChangeCartItemQuantityCommand>
    {
        private readonly ICartProductService _cartProductService;

        public ChangeCartItemQuantityCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartProductService cartProductService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
        }

        public override async Task<CartAggregate> Handle(ChangeCartItemQuantityCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            if (request.Quantity == 0)
            {
                await cartAggregate.RemoveItemAsync(request.LineItemId);
                return await SaveCartAsync(cartAggregate);
            }

            var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id.Equals(request.LineItemId));
            CartProduct product = null;
            if (lineItem != null)
            {
                product = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, new[] { lineItem.ProductId })).FirstOrDefault();
            }

            await cartAggregate.ChangeItemQuantityAsync(new ItemQtyAdjustment
            {
                LineItem = lineItem,
                LineItemId = request.LineItemId,
                NewQuantity = request.Quantity,
                CartProduct = product
            });

            return await SaveCartAsync(cartAggregate);
        }
    }
}
