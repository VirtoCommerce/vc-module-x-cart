using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartProductsLoaderService
    {
        /// <summary>
        /// Load products and fill their inventory data and prices based on specified <see cref="CartProductsRequest"/>
        /// </summary>
        /// <param name="request">Request (cart data to use, product ids)</param>
        /// <returns>List of <see cref="CartProduct"/></returns>
        Task<IList<CartProduct>> GetCartProductsAsync(CartProductsRequest request);
    }
}
