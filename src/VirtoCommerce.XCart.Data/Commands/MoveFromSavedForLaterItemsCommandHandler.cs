using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class MoveFromSavedForLaterItemsCommandHandler(ISavedForLaterListService savedForLaterListService) : IRequestHandler<MoveFromSavedForLaterItemsCommand, CartAggregateWithList>
    {
        public async Task<CartAggregateWithList> Handle(MoveFromSavedForLaterItemsCommand request, CancellationToken cancellationToken)
        {
            return await savedForLaterListService.MoveFromSavedForLaterItems(request);
        }
    }
}
