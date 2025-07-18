using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public interface ISavedForLaterListService
{
    Task<CartAggregate> EnsureSaveForLaterListAsync(ICartRequest request);
    Task<CartAggregate> FindSavedForLaterListAsync(ICartRequest request);
    Task<CartAggregate> CreateSaveForLaterListAsync(ICartRequest request);
}
