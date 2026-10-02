using System;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class OrganizationCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.Organization;

    // The one scope that shares write: every member of the organization is a co-owner of its lists.
    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return CartSharingAccess.Write;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        // A list with no organization is not shared with one, whatever its setting says. Without this an empty
        // organization on both sides compares equal, which hands the list to every user who has none (VCST-6125).
        if (string.IsNullOrEmpty(cart.OrganizationId))
        {
            return IsOwner(cart, currentUserId);
        }

        return !string.IsNullOrEmpty(currentUserId) && cart.OrganizationId.EqualsIgnoreCase(currentOrganizationId);
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        // Checked before anything is written (VCST-6113), and it keeps the organization-less list above
        // out of reach rather than only harmless.
        if (string.IsNullOrEmpty(context.CurrentOrganizationId))
        {
            throw new InvalidOperationException("The Organization sharing scope requires the caller to belong to an organization.");
        }

        EnsureSetting(cart, context.SharingKey);
        SetOrganization(cart, context.CurrentOrganizationId);

        return Task.CompletedTask;
    }

    public override void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria)
    {
        criteria.CustomerId = null;
    }
}
