using System;
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
    public class ChangeCartItemPriceCommandHandler : CartCommandHandler<ChangeCartItemPriceCommand>
    {
        public ChangeCartItemPriceCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ChangeCartItemPriceCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            if (cartAggregate == null)
            {
                var tcs = new TaskCompletionSource<CartAggregate>();
                tcs.SetException(new OperationCanceledException("Cart not found!"));
                return await tcs.Task;
            }

            var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id.Equals(request.LineItemId));
            var priceAdjustment = new PriceAdjustment
            {
                LineItem = lineItem,
                LineItemId = request.LineItemId,
                NewPrice = request.Price
            };

            await cartAggregate.ChangeItemPriceAsync(priceAdjustment);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
