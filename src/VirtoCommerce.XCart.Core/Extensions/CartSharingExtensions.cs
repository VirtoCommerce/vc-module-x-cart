using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class CartSharingExtensions
{
    // The one definition of ownership. Ownership decides who may share, re-scope or remove a list, so it must
    // not drift between the policies, the sharing service and the authorization handler.
    public static bool IsOwnedBy(this ShoppingCart cart, string userId)
    {
        return !string.IsNullOrEmpty(userId) && cart?.CustomerId.EqualsIgnoreCase(userId) == true;
    }

    // The setting that carries the scope, key, targets and message: the first non-Private row (a legacy multi-row
    // cart keeps demoted Private rows), else the first row.
    public static CartSharingSetting GetEffectiveSharingSetting(this ShoppingCart cart)
    {
        var settings = cart?.SharingSettings;

        if (settings.IsNullOrEmpty())
        {
            return null;
        }

        return settings.FirstOrDefault(x => !CartSharingScope.Private.EqualsIgnoreCase(x.Scope)) ?? settings[0];
    }

    // Which ids the set would hold after the change: the current ones minus the removals, plus the non-empty
    // additions, compared case-insensitively and in that order. Pure, so a policy can check the outcome BEFORE
    // anything is written - a write rejected after the fact stays in the cached aggregate (VCST-6113).
    // Sets, not scans: a list may be shared with a thousand recipients and changed by as many ids at once.
    public static IList<string> GetResultingSharedWithIds(this CartSharingSetting setting, IEnumerable<string> addSharedWithIds, IEnumerable<string> removeSharedWithIds)
    {
        var removeIds = (removeSharedWithIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        var kept = (setting?.Targets ?? []).Select(x => x.SharedWithId).Where(x => !removeIds.Contains(x));
        var added = (addSharedWithIds ?? []).Where(x => !string.IsNullOrEmpty(x));

        foreach (var sharedWithId in kept.Concat(added))
        {
            // Add reports whether the id is new, so an id already present - or repeated in the input - is skipped.
            if (resultIds.Add(sharedWithId))
            {
                result.Add(sharedWithId);
            }
        }

        return result;
    }

    // Union with the adds, minus the removes, applied to the stored rows. Built on GetResultingSharedWithIds so
    // the rule a policy validates against and the rule that is written are the same one.
    public static void ApplyTargets(this CartSharingSetting setting, IEnumerable<string> addSharedWithIds, IEnumerable<string> removeSharedWithIds)
    {
        setting.Targets ??= [];

        var resulting = setting.GetResultingSharedWithIds(addSharedWithIds, removeSharedWithIds);
        var resultingIds = resulting.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // By index: Entity.Equals treats two transient (Id-less) rows as equal, so Remove(item) may drop the wrong one.
        for (var i = setting.Targets.Count - 1; i >= 0; i--)
        {
            if (!resultingIds.Contains(setting.Targets[i].SharedWithId))
            {
                setting.Targets.RemoveAt(i);
            }
        }

        var currentIds = setting.Targets.Select(x => x.SharedWithId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sharedWithId in resulting.Where(x => !currentIds.Contains(x)))
        {
            var target = AbstractTypeFactory<CartSharingSettingTarget>.TryCreateInstance();

            target.CartSharingSettingId = setting.Id;
            target.SharedWithId = sharedWithId;

            setting.Targets.Add(target);
        }
    }

    // A single sharedWithId means "this list is shared with exactly this one": the released storefront's picker
    // holds one recipient and sends it on every save, and pre-3.1036 consumers have no other way to say it.
    // Expressed as deltas against the set it refers to: the id it already carries changes nothing (a rename-only
    // save), a single target is replaced, an empty set gains it. More than one target is a set a single-valued
    // caller cannot see, so replacing it would silently revoke - that is refused instead.
    public static (IList<string> Add, IList<string> Remove) GetLegacyTargetDeltas(this CartSharingSetting setting, string sharedWithId)
    {
        var currentIds = (setting?.Targets ?? []).Select(x => x.SharedWithId).ToList();

        // Already shared with this id, wherever it sits in the set: a single-valued client re-sending what it
        // read back. Matching the FIRST target made the answer depend on which response the client kept, since
        // the order differs between a mutation response and a later read (VCST-6152).
        if (currentIds.Contains(sharedWithId, StringComparer.OrdinalIgnoreCase))
        {
            return ([], []);
        }

        if (currentIds.Count > 1)
        {
            throw new InvalidOperationException($"The list is shared with {currentIds.Count} recipients: use addSharedWithIds and removeSharedWithIds to change the set.");
        }

        return ([sharedWithId], currentIds);
    }

    // null keeps the current message, an empty string clears it.
    public static void ApplyMessage(this CartSharingSetting setting, string message)
    {
        if (message != null)
        {
            setting.Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        }
    }

    // The unresolved form: every id the caller asked about comes back, so a recipient whose organization is gone
    // still shows up and can be revoked.
    public static IList<WishlistSharingTarget> ToSharingTargets(this IEnumerable<string> sharedWithIds)
    {
        return (sharedWithIds ?? []).Select(x =>
        {
            var target = AbstractTypeFactory<WishlistSharingTarget>.TryCreateInstance();

            target.Id = x;

            return target;
        }).ToList();
    }

    public static IList<string> GetSharedWithIds(this CartSharingSetting setting)
    {
        return (setting?.Targets ?? []).Select(x => x.SharedWithId).ToList();
    }
}
