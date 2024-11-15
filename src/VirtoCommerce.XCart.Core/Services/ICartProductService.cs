using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartProductService
    {
        /// <summary>
        /// Load products and fill their inventory data and prices based on specified <see cref="CartAggregate"/>
        /// </summary>
        /// <param name="aggregate">Cart data to use</param>
        /// <param name="ids">Product ids</param>
        /// <returns>List of <see cref="CartProduct"/></returns>
        Task<IList<CartProduct>> GetCartProductsByIdsAsync(CartAggregate aggregate, IList<string> ids);
    }

    public interface ICartProductService2
    {
        /// <summary>
        /// Load products and fill their inventory data and prices based on specified <see cref="CartAggregate"/>
        /// </summary>
        /// <param name="aggregate">Cart data to use</param>
        /// <param name="ids">Product ids</param>
        /// <returns>List of <see cref="CartProduct"/></returns>
        Task<IList<CartProduct>> GetCartProductsByIdsAsync(ICartProductContainer aggregate, IList<string> ids, bool loadInventory = true, bool loadPrice = true);
    }
}
