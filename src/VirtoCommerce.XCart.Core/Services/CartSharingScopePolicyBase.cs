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

    // The owner always writes their own list; a share hands out a read-only copy unless the scope says otherwise.
    public virtual string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return IsOwner(cart, currentUserId) ? CartSharingAccess.Write : CartSharingAccess.Read;
    }

    public abstract bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId);

    public virtual Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        return Task.CompletedTask;
    }

    // One effective setting per cart; its Id is the sharing key and survives every scope change.
    public virtual CartSharingSetting EnsureSetting(ShoppingCart cart, string sharingKey)
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

        // What the share hands out, which is what a viewer who is not the owner gets.
        setting.Access = GetAccess(cart, currentUserId: null);

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

    // A scope write never changes who owns the list: the owner is assigned when the list is created and only the
    // owner may write the scope (VCST-6125). The organization does follow the scope - it is what makes a list
    // visible to an organization, so every other scope has to clear it.
    protected virtual void SetOrganization(ShoppingCart cart, string organizationId)
    {
        cart.OrganizationId = organizationId;
    }

    protected static bool IsOwner(ShoppingCart cart, string currentUserId)
    {
        return !string.IsNullOrEmpty(currentUserId) && cart?.CustomerId.EqualsIgnoreCase(currentUserId) == true;
    }
}
