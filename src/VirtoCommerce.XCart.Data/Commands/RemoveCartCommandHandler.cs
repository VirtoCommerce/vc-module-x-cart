using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RemoveCartCommandHandler : IRequestHandler<RemoveCartCommand, bool>
    {
        public RemoveCartCommandHandler(ICartAggregateRepository cartAggregateRepository)
        {
            CartAggrRepository = cartAggregateRepository;
        }

        private ICartAggregateRepository CartAggrRepository { get; set; }

        public virtual async Task<bool> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
        {
            await CartAggrRepository.RemoveCartAsync(request.CartId);

            return true;
        }
    }
}
