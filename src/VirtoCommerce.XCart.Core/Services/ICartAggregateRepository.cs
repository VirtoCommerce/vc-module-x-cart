using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartAggregateRepository
    {
        Task RemoveCartAsync(string cartId);

        Task SaveAsync(CartAggregate cartAggregate);

        Task<CartAggregate> GetCartAsync(ICartRequest cartRequest, string responseGroup = null);

        Task<CartAggregate> GetCartAsync(ShoppingCartSearchCriteria criteria, string cultureName);

        Task<CartAggregate> GetCartAsync(ShoppingCartSearchCriteria criteria, IList<string> productsIncludeFields, string cultureName);

        Task<CartAggregate> GetCartByIdAsync(string cartId, string cultureName = null);

        Task<CartAggregate> GetCartByIdAsync(string cartId, IList<string> productsIncludeFields, string cultureName = null);

        Task<CartAggregate> GetCartByIdAsync(string cartId, string responseGroup, IList<string> productsIncludeFields, string cultureName = null);

        Task<CartAggregate> GetCartForShoppingCartAsync(ShoppingCart cart, string cultureName = null);

        Task<SearchCartResponse> SearchCartAsync(ShoppingCartSearchCriteria criteria);

        Task<SearchCartResponse> SearchCartAsync(ShoppingCartSearchCriteria criteria, IList<string> productsIncludeFields);
    }
}
