using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services;

public abstract class CartSharingScopePolicyBase : ICartSharingScopePolicy
{
    public abstract string Scope { get; }

    public virtual string Description => $"{Scope} scope";

    public virtual bool CanApply => true;

    public virtual string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return CartSharingAccess.Read;
    }

    public abstract bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId);

    public virtual Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        return Task.CompletedTask;
    }

    // Default keeps one effective setting: rows demoted to Private, the first carries the scope. Override for many.
    public virtual void EnsureSetting(ShoppingCart cart, string sharingKey, string access, string sharedWithId)
    {
        if (cart.SharingSettings.IsNullOrEmpty())
        {
            cart.SharingSettings ??= [];

            var newSetting = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            newSetting.Id = sharingKey;
            newSetting.ShoppingCartId = cart.Id;
            newSetting.Scope = Scope;
            newSetting.Access = access;
            newSetting.SharedWithId = sharedWithId;

            cart.SharingSettings.Add(newSetting);

            return;
        }

        foreach (var setting in cart.SharingSettings)
        {
            setting.Scope = CartSharingScope.Private;
        }

        // Id untouched: an existing sharing key must survive a scope change.
        var sharingSetting = cart.SharingSettings.First();

        sharingSetting.Scope = Scope;
        sharingSetting.Access = access;
        sharingSetting.SharedWithId = sharedWithId;
    }

    public virtual void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria)
    {
    }

    protected virtual void SetOwner(ShoppingCart cart, string userId, string customerName, string organizationId)
    {
        cart.CustomerId = userId;
        cart.CustomerName = customerName;
        cart.OrganizationId = organizationId;
    }

    protected static bool IsOwner(ShoppingCart cart, string currentUserId)
    {
        return !string.IsNullOrEmpty(currentUserId) && cart?.CustomerId.EqualsIgnoreCase(currentUserId) == true;
    }
}
