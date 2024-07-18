using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateCartPaymentDynamicPropertiesCommandHandler : CartCommandHandler<UpdateCartPaymentDynamicPropertiesCommand>
    {
        public UpdateCartPaymentDynamicPropertiesCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(UpdateCartPaymentDynamicPropertiesCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.UpdateCartPaymentDynamicProperties(request.PaymentId, request.DynamicProperties);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
