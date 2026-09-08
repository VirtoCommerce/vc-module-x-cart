using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingService : ICartSharingService
{
    private readonly ICartAggregateRepository _cartAggregateRepository;
    private readonly IReadOnlyDictionary<string, ICartSharingScopePolicy> _scopePolicies;

    public CartSharingService(ICartAggregateRepository cartAggregateRepository, IEnumerable<ICartSharingScopePolicy> scopePolicies)
    {
        _cartAggregateRepository = cartAggregateRepository;
        _scopePolicies = BuildScopePolicyIndex(scopePolicies);
    }

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

        return FindScopePolicy(cart)?.Scope ?? CartSharingScope.Private;
    }

    public virtual string GetSharingAccess(ShoppingCart cart, string currentUserId)
    {
        return _scopePolicies.TryGetValue(GetSharingScope(cart), out var policy)
            ? policy.GetAccess(cart, currentUserId)
            : CartSharingAccess.Read;
    }

    public virtual bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        // Resolved from the persisted settings only — never from the "no settings + OrganizationId => Organization"
        // inference GetSharingScope makes, which would authorize an org member on a cart that was never shared.
        var policy = FindScopePolicy(cart);

        return policy != null
            ? policy.IsAuthorized(cart, currentUserId, currentOrganizationId)
            : IsOwner(cart, currentUserId);
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

    [Obsolete("Use the overload with sharedWithId (null for the built-in non-targeted scopes).", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access)
    {
        EnsureSharingSettings(cart, sharingKey, mode, access, sharedWithId: null);
    }

    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access, string sharedWithId)
    {
        // Routed through the scope's policy so its own write behavior applies, whoever owns that scope.
        if (string.IsNullOrEmpty(mode) || !_scopePolicies.TryGetValue(mode, out var policy))
        {
            throw new InvalidOperationException($"Unsupported sharing scope '{mode}'.");
        }

        policy.EnsureSetting(cart, sharingKey, access, sharedWithId);
    }

    public virtual Task UpdateScopeAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        if (string.IsNullOrEmpty(context.Scope))
        {
            return Task.CompletedTask;
        }

        if (!_scopePolicies.TryGetValue(context.Scope, out var policy) || !policy.CanApply)
        {
            throw new InvalidOperationException($"Unsupported sharing scope '{context.Scope}'.");
        }

        return policy.ApplyAsync(cart, context);
    }

    public virtual void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria, string scope)
    {
        if (!string.IsNullOrEmpty(scope) && _scopePolicies.TryGetValue(scope, out var policy))
        {
            policy.ConfigureSearchCriteria(criteria);
        }
    }

    public virtual async Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields)
    {
        var cartSearchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();

        cartSearchCriteria.SharingKey = sharingKey;
        cartSearchCriteria.Take = 1;

        var searchResult = await _cartAggregateRepository.SearchCartAsync(cartSearchCriteria, includeFields);
        return searchResult.Results.FirstOrDefault();
    }

    protected virtual ICartSharingScopePolicy FindScopePolicy(ShoppingCart cart)
    {
        if (cart == null || cart.SharingSettings.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var setting in cart.SharingSettings)
        {
            if (!string.IsNullOrEmpty(setting.Scope) && _scopePolicies.TryGetValue(setting.Scope, out var policy))
            {
                return policy;
            }
        }

        return null;
    }

    private static bool IsOwner(ShoppingCart cart, string currentUserId)
    {
        return !string.IsNullOrEmpty(currentUserId) && cart?.CustomerId == currentUserId;
    }

    private static IReadOnlyDictionary<string, ICartSharingScopePolicy> BuildScopePolicyIndex(IEnumerable<ICartSharingScopePolicy> scopePolicies)
    {
        var result = new Dictionary<string, ICartSharingScopePolicy>(StringComparer.OrdinalIgnoreCase);

        foreach (var policy in scopePolicies)
        {
            if (string.IsNullOrEmpty(policy.Scope))
            {
                throw new InvalidOperationException($"{policy.GetType().FullName} must declare a non-empty {nameof(ICartSharingScopePolicy.Scope)}.");
            }

            if (!result.TryAdd(policy.Scope, policy))
            {
                throw new InvalidOperationException(
                    $"Two {nameof(ICartSharingScopePolicy)} implementations claim the sharing scope '{policy.Scope}': " +
                    $"{result[policy.Scope].GetType().FullName} and {policy.GetType().FullName}.");
            }
        }

        return result;
    }
}
