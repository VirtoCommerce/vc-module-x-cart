using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Extensions;
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

    // One effective setting per cart; its Id is the sharing key and survives every scope change.
    public virtual CartSharingSetting EnsureSetting(ShoppingCart cart, string sharingKey, string access)
    {
        cart.SharingSettings ??= [];

        var setting = cart.GetEffectiveSharingSetting();

        if (setting == null)
        {
            setting = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            setting.Id = sharingKey;
            setting.ShoppingCartId = cart.Id;
            setting.Scope = Scope;

            cart.SharingSettings.Add(setting);
        }
        else
        {
            for (var i = cart.SharingSettings.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(cart.SharingSettings[i], setting))
                {
                    cart.SharingSettings.RemoveAt(i);
                }
            }

            // Targets and the message belong to the scope that wrote them.
            if (!Scope.EqualsIgnoreCase(setting.Scope))
            {
                setting.Scope = Scope;
                setting.Targets = [];
                setting.Message = null;
            }
        }

        setting.Access = access;

        return setting;
    }

    // Ids of one scope, batched across every list in the request: resolve them in one call, not one per list.
    public virtual Task<IList<WishlistSharingTarget>> ResolveTargetsAsync(IList<string> sharedWithIds)
    {
        return Task.FromResult(sharedWithIds.ToSharingTargets());
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
