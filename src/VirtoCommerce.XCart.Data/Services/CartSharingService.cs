using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using CartModuleConstants = VirtoCommerce.CartModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingService : ICartSharingService
{
    private readonly ICartAggregateRepository _cartAggregateRepository;
    private readonly Dictionary<string, ICartSharingScopePolicy> _scopePolicies;

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
        // Settings only: GetSharingScope's no-settings => Organization inference would authorize any org member.
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

    [Obsolete("Use UpdateScopeAsync.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access)
    {
        EnsureSharingSettings(cart, sharingKey, mode, access, sharedWithId: null);
    }

    [Obsolete("Use UpdateScopeAsync.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public virtual void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access, string sharedWithId)
    {
        // Through the scope's policy, so its owner's write behavior applies.
        if (string.IsNullOrEmpty(mode) || !_scopePolicies.TryGetValue(mode, out var policy))
        {
            throw new InvalidOperationException($"Unsupported sharing scope '{mode}'.");
        }

        var setting = policy.EnsureSetting(cart, sharingKey, access);

        if (!string.IsNullOrEmpty(sharedWithId))
        {
            // EnsureSetting has already cleared the targets if the scope changed.
            var (add, remove) = setting.GetLegacyTargetDeltas(sharedWithId);

            setting.ApplyTargets(add, remove);
        }
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

        ApplyLegacySharedWithId(cart, context);
        ValidateContext(context);

        return policy.ApplyAsync(cart, context);
    }

    public virtual Task<IList<WishlistSharingTarget>> ResolveTargetsAsync(string scope, IList<string> sharedWithIds)
    {
        if (sharedWithIds.IsNullOrEmpty())
        {
            return Task.FromResult<IList<WishlistSharingTarget>>([]);
        }

        return !string.IsNullOrEmpty(scope) && _scopePolicies.TryGetValue(scope, out var policy)
            ? policy.ResolveTargetsAsync(sharedWithIds)
            : Task.FromResult(sharedWithIds.ToSharingTargets());
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

    // A client that speaks deltas gets delta semantics; a single-valued one gets single-target semantics.
    protected virtual void ApplyLegacySharedWithId(ShoppingCart cart, WishlistScopeContext context)
    {
        if (string.IsNullOrEmpty(context.LegacySharedWithId))
        {
            return;
        }

        if (!context.AddSharedWithIds.IsNullOrEmpty() || !context.RemoveSharedWithIds.IsNullOrEmpty())
        {
            context.AddSharedWithIds = [.. context.AddSharedWithIds ?? [], context.LegacySharedWithId];
            return;
        }

        // A scope change clears the targets, so the id lands in an empty set whatever the list carried before.
        var setting = cart.GetEffectiveSharingSetting();
        var current = context.Scope.EqualsIgnoreCase(setting?.Scope) ? setting : null;

        (context.AddSharedWithIds, context.RemoveSharedWithIds) = current.GetLegacyTargetDeltas(context.LegacySharedWithId);
    }

    protected virtual void ValidateContext(WishlistScopeContext context)
    {
        if (context.Message?.Length > CartModuleConstants.Sharing.MessageMaxLength)
        {
            throw new InvalidOperationException($"The sharing message must not exceed {CartModuleConstants.Sharing.MessageMaxLength} characters.");
        }

        // Adds only: an added id costs an authorization check and a persisted row, a removed one costs neither.
        if (context.AddSharedWithIds?.Count > CartModuleConstants.Sharing.MaxTargets)
        {
            throw new InvalidOperationException($"A list cannot be shared with more than {CartModuleConstants.Sharing.MaxTargets} targets in one write.");
        }

        var conflictingIds = (context.AddSharedWithIds ?? []).Intersect(context.RemoveSharedWithIds ?? [], StringComparer.OrdinalIgnoreCase).ToList();

        if (conflictingIds.Count > 0)
        {
            throw new InvalidOperationException($"Sharing targets cannot be both added and removed: {string.Join(", ", conflictingIds)}.");
        }
    }

    // The effective setting's policy: non-Private rows first (a legacy multi-row cart keeps demoted Private rows),
    // skipping scopes without a registered policy - fail-closed, never wider.
    protected virtual ICartSharingScopePolicy FindScopePolicy(ShoppingCart cart)
    {
        if (cart == null || cart.SharingSettings.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var setting in cart.SharingSettings.OrderBy(x => CartSharingScope.Private.EqualsIgnoreCase(x.Scope)))
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
        return !string.IsNullOrEmpty(currentUserId) && cart?.CustomerId.EqualsIgnoreCase(currentUserId) == true;
    }

    private static Dictionary<string, ICartSharingScopePolicy> BuildScopePolicyIndex(IEnumerable<ICartSharingScopePolicy> scopePolicies)
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
