using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class CartSharingExtensions
{
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

    // Union with the adds, minus the removes; ids are compared case-insensitively.
    public static void ApplyTargets(this CartSharingSetting setting, IEnumerable<string> addSharedWithIds, IEnumerable<string> removeSharedWithIds)
    {
        setting.Targets ??= [];

        // Sets, not scans: a list may be shared with a thousand recipients and changed by as many ids at once.
        var removeIds = (removeSharedWithIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // By index: Entity.Equals treats two transient (Id-less) rows as equal, so Remove(item) may drop the wrong one.
        for (var i = setting.Targets.Count - 1; i >= 0; i--)
        {
            if (removeIds.Contains(setting.Targets[i].SharedWithId))
            {
                setting.Targets.RemoveAt(i);
            }
        }

        var currentIds = setting.Targets.Select(x => x.SharedWithId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sharedWithId in (addSharedWithIds ?? []).Where(x => !string.IsNullOrEmpty(x)))
        {
            // Add reports whether the id is new, so an id already shared with - or repeated in the input - is skipped.
            if (!currentIds.Add(sharedWithId))
            {
                continue;
            }

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

        if (currentIds.FirstOrDefault().EqualsIgnoreCase(sharedWithId))
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
