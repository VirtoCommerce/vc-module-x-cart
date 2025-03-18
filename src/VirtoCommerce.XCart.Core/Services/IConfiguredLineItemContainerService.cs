using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface IConfiguredLineItemContainerService
    {
        Task<ConfiguredLineItemContainer> CreateContainerAsync(ICartProductContainerRequest request);
    }
}
