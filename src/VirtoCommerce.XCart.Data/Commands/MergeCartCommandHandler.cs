using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class MergeCartCommandHandler : CartCommandHandler<MergeCartCommand>
    {
        public MergeCartCommandHandler(ICartAggregateRepository cartRepository)
            : base(cartRepository)
        {
        }

        public override async Task<CartAggregate> Handle(MergeCartCommand request, CancellationToken cancellationToken)
        {
            var cartAggr = await GetOrCreateCartFromCommandAsync(request);
            var secondCart = await GetCartById(request.SecondCartId, request.CultureName);
            if (secondCart != null && secondCart.Id != cartAggr.Id)
            {
                cartAggr = await cartAggr.MergeWithCartAsync(secondCart);
                await CartRepository.SaveAsync(cartAggr);
                if (request.DeleteAfterMerge)
                {
                    await CartRepository.RemoveCartAsync(secondCart.Id);
                }
            }
            return cartAggr;
        }
    }
}
