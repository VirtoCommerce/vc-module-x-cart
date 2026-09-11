using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class SharingTargetDataLoaderExtensions
{
    private const string LoaderKey = "cart_sharing_targets";

    private static readonly DataLoaderResult<WishlistSharingTarget[]> _noTargets = new([]);

    public static IDataLoaderResult<WishlistSharingTarget[]> NoTargets => _noTargets;

    /// <summary>
    /// Resolves the recipients of one sharing setting through a request-scoped batch loader: a page of wishlists
    /// resolves its recipients in one call per scope instead of one call per list, and an organization several
    /// lists are shared with is resolved once. The key carries the scope because the id space belongs to the
    /// scope, not to the module - two scopes may legitimately use the same id for different principals.
    /// </summary>
    public static IDataLoaderResult<WishlistSharingTarget[]> LoadSharingTargets(
        this IDataLoaderContextAccessor dataLoader,
        ICartSharingService cartSharingService,
        CartSharingSetting setting)
    {
        var sharedWithIds = setting.GetSharedWithIds();

        if (sharedWithIds.Count == 0)
        {
            return _noTargets;
        }

        var loader = dataLoader.Context.GetOrAddBatchLoader<(string Scope, string SharedWithId), WishlistSharingTarget>(
            LoaderKey,
            async keys =>
            {
                var result = new Dictionary<(string Scope, string SharedWithId), WishlistSharingTarget>();

                foreach (var scopeKeys in keys.GroupBy(x => x.Scope, StringComparer.OrdinalIgnoreCase))
                {
                    var ids = scopeKeys.Select(x => x.SharedWithId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var targets = await cartSharingService.ResolveTargetsAsync(scopeKeys.Key, ids);

                    var targetsById = new Dictionary<string, WishlistSharingTarget>(StringComparer.OrdinalIgnoreCase);
                    foreach (var target in targets)
                    {
                        targetsById[target.Id] = target;
                    }

                    foreach (var key in scopeKeys)
                    {
                        if (targetsById.TryGetValue(key.SharedWithId, out var target))
                        {
                            result[key] = target;
                        }
                    }
                }

                return result;
            },
            keyComparer: AnonymousComparer.Create(((string Scope, string SharedWithId) x) => $"{x.Scope}:{x.SharedWithId}"));

        return loader.LoadAsync(sharedWithIds.Select(x => (setting.Scope, x)));
    }
}
