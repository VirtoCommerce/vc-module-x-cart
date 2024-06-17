using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateCartItemDynamicPropertiesCommandHandler : CartCommandHandler<UpdateCartItemDynamicPropertiesCommand>
    {
        public UpdateCartItemDynamicPropertiesCommandHandler(ICartAggregateRepository cartRepository)
            : base(cartRepository)
        {
        }

        public override async Task<CartAggregate> Handle(UpdateCartItemDynamicPropertiesCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.UpdateCartItemDynamicProperties(request.LineItemId, request.DynamicProperties);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
