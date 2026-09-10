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

        var removeIds = removeSharedWithIds?.ToList() ?? [];

        // By index: Entity.Equals treats two transient (Id-less) rows as equal, so Remove(item) may drop the wrong one.
        for (var i = setting.Targets.Count - 1; i >= 0; i--)
        {
            if (removeIds.Contains(setting.Targets[i].SharedWithId, StringComparer.OrdinalIgnoreCase))
            {
                setting.Targets.RemoveAt(i);
            }
        }

        foreach (var sharedWithId in (addSharedWithIds ?? []).Where(x => !string.IsNullOrEmpty(x)))
        {
            if (setting.Targets.Any(x => x.SharedWithId.EqualsIgnoreCase(sharedWithId)))
            {
                continue;
            }

            var target = AbstractTypeFactory<CartSharingSettingTarget>.TryCreateInstance();

            target.CartSharingSettingId = setting.Id;
            target.SharedWithId = sharedWithId;

            setting.Targets.Add(target);
        }
    }

    // null keeps the current message, an empty string clears it.
    public static void ApplyMessage(this CartSharingSetting setting, string message)
    {
        if (message != null)
        {
            setting.Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        }
    }

    public static IList<WishlistSharingTarget> ToSharingTargets(this CartSharingSetting setting)
    {
        return (setting?.Targets ?? []).Select(x =>
        {
            var target = AbstractTypeFactory<WishlistSharingTarget>.TryCreateInstance();

            target.Id = x.SharedWithId;

            return target;
        }).ToList();
    }
}
