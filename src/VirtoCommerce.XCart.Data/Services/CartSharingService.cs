using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingService(ICartAggregateRepository cartAggregateRepository) : ICartSharingService
{
    public virtual string GetSharingScope(ShoppingCart cart)
    {
        if (cart == null)
        {
            return CartSharingScope.Private;
        }

        if (cart.SharingSettings.IsNullOrEmpty())
        {
            return string.IsNullOrEmpty(cart.OrganizationId) ? CartSharingScope.Private : CartSharingScope.Organization;
        }

        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return CartSharingScope.AnyoneAnonymous;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return CartSharingScope.AnyoneAuthorized;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return CartSharingScope.Organization;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.User))
        {
            return CartSharingScope.User;
        }

        return CartSharingScope.Private;
    }

    public virtual string GetSharingAccess(ShoppingCart cart, string currentUserId)
    {
        var sharingScope = GetSharingScope(cart);

        if (sharingScope == CartSharingScope.Private || sharingScope == CartSharingScope.Organization)
        {
            return CartSharingAccess.Write;
        }
        else if (sharingScope == CartSharingScope.AnyoneAnonymous || sharingScope == CartSharingScope.AnyoneAuthorized)
        {
            return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerUserId(cart) == currentUserId ? CartSharingAccess.Write : CartSharingAccess.Read;
        }
        else
        {
            return CartSharingAccess.Read;
        }
    }

    public virtual bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return true;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return !string.IsNullOrEmpty(currentUserId);
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerOrganizationId(cart) == currentOrganizationId;
        }

        return !string.IsNullOrEmpty(currentUserId) && GetSharingOwnerUserId(cart) == currentUserId;
    }

    public virtual void SetOwner(ShoppingCart cart, string userId, string customerName, string organizationId)
    {
        cart.CustomerId = userId;
        cart.CustomerName = customerName;
        cart.OrganizationId = organizationId;
    }

    public virtual string GetSharingOwnerUserId(ShoppingCart cart)
    {
        return cart.CustomerId;
    }

    public virtual string GetSharingOwnerOrganizationId(ShoppingCart cart)
    {
        return cart.OrganizationId;
    }

    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access, string sharedWithId = null)
    {
        if (cart.SharingSettings.IsNullOrEmpty())
        {
            var sharingSetting = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            sharingSetting.Id = sharingKey;
            sharingSetting.ShoppingCartId = cart.Id;
            sharingSetting.Scope = mode;
            sharingSetting.Access = access;
            sharingSetting.SharedWithId = sharedWithId;

            cart.SharingSettings.Add(sharingSetting);
        }
        else
        {
            foreach (var setting in cart.SharingSettings)
            {
                setting.Scope = CartSharingScope.Private;
            }

            var sharingSetting = cart.SharingSettings.First();

            sharingSetting.Scope = mode;
            sharingSetting.Access = access;
            sharingSetting.SharedWithId = sharedWithId;
        }
    }

    public virtual Task UpdateScopeAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        // A null/empty scope means "don't touch sharing" (e.g. a rename-only edit); an unrecognized scope (ApplyScope
        // returned false) is rejected. The base pipeline has no authorization concern — a module that introduces an
        // authorizable scope overrides this method to gate it before delegating to base.
        if (!string.IsNullOrEmpty(context.Scope) && !ApplyScope(cart, context))
        {
            throw new InvalidOperationException($"Unsupported sharing scope '{context.Scope}'.");
        }

        return Task.CompletedTask;
    }

    // Applies a recognized scope to the cart (sharing setting + owner) and returns true; returns false for a scope
    // this pipeline does not know, which makes UpdateScopeAsync throw. A module that adds a scope overrides this,
    // handles its own scope, and delegates the rest to base.
    protected virtual bool ApplyScope(ShoppingCart cart, WishlistScopeContext context)
    {
        if (CartSharingScope.AnyoneAnonymous.EqualsIgnoreCase(context.Scope))
        {
            EnsureSharingSettings(cart, context.SharingKey, CartSharingScope.AnyoneAnonymous, CartSharingAccess.Read);
            SetOwner(cart, context.CurrentUserId, context.CustomerName, null);
            return true;
        }

        if (CartSharingScope.Organization.EqualsIgnoreCase(context.Scope))
        {
            EnsureSharingSettings(cart, context.SharingKey, CartSharingScope.Organization, CartSharingAccess.Write);
            SetOwner(cart, context.CurrentUserId, context.CustomerName, context.CurrentOrganizationId);
            return true;
        }

        if (CartSharingScope.Private.EqualsIgnoreCase(context.Scope))
        {
            EnsureSharingSettings(cart, null, CartSharingScope.Private, CartSharingAccess.Write);
            SetOwner(cart, context.CurrentUserId, context.CustomerName, null);
            return true;
        }

        return false;
    }

    public virtual async Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields)
    {
        var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

        cartSearchCriteria.SharingKey = sharingKey;
        cartSearchCriteria.Take = 1;

        var searchResult = await cartAggregateRepository.SearchCartAsync(cartSearchCriteria, includeFields);
        return searchResult.Results.FirstOrDefault();
    }
}
