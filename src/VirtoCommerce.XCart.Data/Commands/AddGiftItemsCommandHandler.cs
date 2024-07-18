using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddGiftItemsCommandHandler : CartCommandHandler<AddGiftItemsCommand>
    {
        private readonly ICartAvailMethodsService _cartAvailMethodsService;


        public AddGiftItemsCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartAvailMethodsService cartAvailMethodsService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodsService = cartAvailMethodsService;
        }

        public override async Task<CartAggregate> Handle(AddGiftItemsCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.AddGiftItemsAsync(request.Ids, (await _cartAvailMethodsService.GetAvailableGiftsAsync(cartAggregate)).ToList());

            return await SaveCartAsync(cartAggregate);
        }
    }
}
