using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddCartAddressCommandHandler : CartCommandHandler<AddCartAddressCommand>
    {
        public AddCartAddressCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(AddCartAddressCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var address = cartAggregate.Cart.Addresses.FirstOrDefault(x => (int)x.AddressType == request.Address.AddressType?.Value);
            address = request.Address.MapTo(address);

            await cartAggregate.AddOrUpdateCartAddressByTypeAsync(address);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
