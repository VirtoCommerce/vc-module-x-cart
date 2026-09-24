using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public interface ICartSharingScopePolicy
{
    string Scope { get; }

    string Description { get; }

    bool CanApply { get; }

    string GetAccess(ShoppingCart cart, string currentUserId);

    bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId);

    Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context);

    void EnsureSetting(ShoppingCart cart, string sharingKey, string access, string sharedWithId);

    void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria);
}
