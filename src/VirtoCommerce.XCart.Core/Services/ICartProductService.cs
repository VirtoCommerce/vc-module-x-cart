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

        /// <summary>
        /// Load products with their inventory data and prices for the given currency/product pairs.
        /// Supports the same product in several currencies — each pair yields a distinct <see cref="CartProduct"/>
        /// loaded for its currency.
        /// </summary>
        /// <param name="aggregate">Cart data to use</param>
        /// <param name="currencyProductIdPairs">Pairs of (currencyCode, productId). An empty currencyCode falls back to the cart's currency.</param>
        /// <returns>
        /// Products keyed by <see cref="CartAggregate.FormatGetCartProductKey(string, string)"/> ("{productId}:{CURRENCYCODE}").
        /// </returns>
        Task<IDictionary<string, CartProduct>> GetCartProductsAsync(CartAggregate aggregate, IList<(string CurrencyCode, string ProductId)> currencyProductIdPairs);
    }
}
