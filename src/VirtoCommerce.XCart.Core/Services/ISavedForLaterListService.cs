using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public interface ISavedForLaterListService
{
    Task<CartAggregateWithList> MoveFromSavedForLaterItems(MoveSavedForLaterItemsCommandBase request);
    Task<CartAggregateWithList> MoveToSavedForLaterItems(MoveSavedForLaterItemsCommandBase request);

    Task<CartAggregate> FindSavedForLaterListAsync(ICartRequest request);
}
