using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class MoveToSavedForLaterItemsCommandHandler(ISavedForLaterListService savedForLaterListService) : IRequestHandler<MoveToSavedForLaterItemsCommand, CartAggregateWithList>
    {
        public async Task<CartAggregateWithList> Handle(MoveToSavedForLaterItemsCommand request, CancellationToken cancellationToken)
        {
            return await savedForLaterListService.MoveToSavedForLaterItems(request);
        }
    }
}
