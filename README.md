# Virto Commerce Cart Experience API (xCart) Module

[![CI status](https://github.com/VirtoCommerce/vc-module-x-cart/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-x-cart/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cart&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cart) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cart&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cart) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cart&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cart) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cart&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cart)

The xCart module provides high-performance API for shopping carts with the following key features:
* Working with a shopping cart.
* Auto evaluating taxes and prices.
* Multi-language and multi-currency capabilities.
* Lazy resolving.

## Extensibility: adding a wishlist sharing scope

Wishlist sharing scopes are an **additive registry**. A module contributes a scope by registering an
`ICartSharingScopePolicy`; it does not replace `ICartSharingService`, so several modules can add scopes
independently and none of them silently loses to another.

A list carries **one** `CartSharingSetting` (its `Id` is the sharing key in `/shared-list/{key}`, stable for the
life of the list) with the scope, the viewer-independent `Access`, an optional `Message` and a set of `Targets` -
one `CartSharingSettingTarget` per id the list is shared with. The GraphQL inputs change the set with
`addSharedWithIds` / `removeSharedWithIds` (deltas, never a replace-set) and carry the `message` (max 1024
characters). The legacy `sharedWithId` keeps its single-target meaning: it replaces the one target the list has,
changes nothing when it already names it, and is refused when the list has several - a single-valued client cannot
see the set it would otherwise revoke. Sharing is only touched by a write that also carries `scope` - a mutation
without it leaves the scope, the targets and the message as they were.

Derive from `CartSharingScopePolicyBase`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

// A list the owner publishes to partner organizations, read-only for their members.
public class PartnerCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => "Partner";

    public override string Description => "Shared with specific partner organizations";

    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return IsOwner(cart, currentUserId) ? CartSharingAccess.Write : CartSharingAccess.Read;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            return false;
        }

        if (IsOwner(cart, currentUserId))
        {
            return true;
        }

        // Targets are the scope's own id space - partner organization ids here.
        var setting = cart.GetEffectiveSharingSetting();

        return !string.IsNullOrEmpty(currentOrganizationId)
            && setting?.Scope.EqualsIgnoreCase(Scope) == true
            && setting.Targets?.Any(x => x.SharedWithId.EqualsIgnoreCase(currentOrganizationId)) == true;
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        // Authorize context.AddSharedWithIds here if the scope needs it; never the removals - an owner must always be able to revoke.
        var setting = EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read);

        setting.ApplyTargets(context.AddSharedWithIds, context.RemoveSharedWithIds);
        setting.ApplyMessage(context.Message);

        SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

        return Task.CompletedTask;
    }

    public override async Task<IList<WishlistSharingTarget>> ResolveTargetsAsync(IList<string> sharedWithIds)
    {
        // Optional: fill Name / Subtitle / ImageUrl so the storefront can render the recipients. The ids of every
        // list in the request arrive together, so resolve them in ONE call - never one lookup per id.
        var targets = await base.ResolveTargetsAsync(sharedWithIds);

        foreach (var target in targets)
        {
            target.Name = $"Partner {target.Id}";
        }

        return targets;
    }

    public override void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria)
    {
        criteria.OrganizationId = null;
    }
}
```

Register it in your module's `Initialize`:

```csharp
serviceCollection.AddTransient<ICartSharingScopePolicy, PartnerCartSharingScopePolicy>();
```

That is all: no `ICartSharingService` override and no GraphQL enum override. The value is added to the
`WishlistScopeType` enum automatically, so `wishlists(scope: "Partner")` and the wishlist mutations accept it.

### What each member controls

| Member | Controls |
|---|---|
| `Scope` | The stored `CartSharingSetting.Scope` value and the GraphQL enum value. Must be a valid GraphQL enum value name (`[_A-Za-z][_0-9A-Za-z]*`). |
| `Description` | The enum value's schema description. Defaults to `"<Scope> scope"`. |
| `CanApply` | `false` for a scope that can be read but never set - `UpdateScopeAsync` then rejects it. Defaults to `true`. |
| `GetAccess` | `Read` or `Write` for the current caller. Defaults to `Read`. |
| `IsAuthorized` | Whether the caller may see the cart. Fail closed. |
| `ApplyAsync` | Writes the scope onto the cart: `EnsureSetting` for the row, `ApplyTargets` / `ApplyMessage` for a targeted scope. Any authorization for *setting* the scope belongs here. |
| `EnsureSetting` | How the setting row is written. The default keeps one effective setting per cart (its `Id` is the sharing key), drops legacy extra rows, and resets the targets and the message when the scope changes. |
| `ResolveTargetsAsync` | Display data (`Name`, `Subtitle`, `ImageUrl`) for the `targets` of a `sharingSetting`. Defaults to the ids only. Return one target per id you are given; an id you drop is filled back in with its bare id, so a recipient whose principal no longer exists is always visible and revocable. It is called through a request-scoped batch loader: the ids of **every list in the request** arrive in one call per scope, so a page of wishlists costs one resolve, not one per list. Resolved for the **list owner only** - `sharingSetting.targets` is `[]` and `sharedWithId` is `null` for every other viewer, so a recipient never learns who else the list was shared with; `message` stays visible to recipients. |
| `ConfigureSearchCriteria` | How `wishlists(scope: ...)` narrows its search. Defaults to no narrowing. |

Two policies claiming the same `Scope` throw at startup naming both types, so a collision is never silent.
Scope lookup is case-insensitive, so compare stored scope values with `EqualsIgnoreCase` as above.
`Targets` are the scope's own id space (partner organization ids here); the built-in scopes keep the set empty
and ignore any ids or message a caller passes with them. `UpdateScopeAsync` rejects an id that is both added and
removed, a message longer than `VirtoCommerce.CartModule.Core.ModuleConstants.Sharing.MessageMaxLength` (1024), and
an add list longer than `ModuleConstants.Sharing.MaxTargets` (1000) - one write persists one row per id.

## Documentation

* [xCart documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/Cart/overview/)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-x-cart)
* [Experience API Documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/)
* [Getting started](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/getting-started/)
* [How to use GraphiQL](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/graphiql/)
* [How to use Postman](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/postman/)
* [How to extend](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/x-api-extensions/)
* [Virto Commerce Frontend architecture](https://docs.virtocommerce.org/storefront/developer-guide/architecture/)
* [How to measure a change with the benchmarks](benchmarks/VirtoCommerce.XCart.Benchmark/README.md)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-x-cart/releases/latest)


## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
