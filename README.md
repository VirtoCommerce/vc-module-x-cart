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

Derive from `CartSharingScopePolicyBase`:

```csharp
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

// A list the owner publishes to one partner organization, read-only for that organization's members.
public class PartnerCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => "Partner";

    public override string Description => "Shared with a specific partner organization";

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

        return IsOwner(cart, currentUserId)
            || (!string.IsNullOrEmpty(currentOrganizationId)
                && cart.SharingSettings?.Any(x => x.Scope.EqualsIgnoreCase(Scope)
                    && x.SharedWithId.EqualsIgnoreCase(currentOrganizationId)) == true);
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        // context.SharedWithId carries the target - the partner organization id for this scope.
        EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read, context.SharedWithId);
        SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

        return Task.CompletedTask;
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
| `ApplyAsync` | Writes the scope onto the cart. Any authorization for *setting* the scope belongs here. |
| `EnsureSetting` | How the setting row is written. The default keeps one effective setting per cart; override to keep several rows for one scope. |
| `ConfigureSearchCriteria` | How `wishlists(scope: ...)` narrows its search. Defaults to no narrowing. |

Two policies claiming the same `Scope` throw at startup naming both types, so a collision is never silent.
Scope lookup is case-insensitive, so compare stored scope values with `EqualsIgnoreCase` as above.
`SharedWithId` is the scope's own id space (a partner organization id here); the built-in scopes leave it `null`.

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
