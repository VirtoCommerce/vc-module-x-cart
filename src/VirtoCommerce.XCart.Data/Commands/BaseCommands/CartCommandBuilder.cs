using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Data.Schemas;

namespace VirtoCommerce.XCart.Data.Commands.BaseCommands;

/// <summary>
/// Base class for cart mutation command builders that handles:
/// - UserId fallback from the current user context (preserves client-provided UserId for admin/LoginOnBehalf scenarios)
/// - OrganizationId assignment from the current user context
/// - Anonymous access check
/// - Cart resolution and authorization (CanAccessCartAuthorizationRequirement)
/// - Distributed lock by UserId through the Platform IDistributedLock (wait: VirtoCommerce:GraphQLDistributedLock:Timeout)
/// - Setting expanded object graph for nested GraphQL resolvers
/// </summary>
public abstract class CartCommandBuilder<TCommand, TInputType>(
    IAuthorizationService authorizationService,
    IDistributedLock distributedLock,
    ICartAggregateRepository cartRepository)
    : CommandBuilder<TCommand, CartAggregate, TInputType, CartType>(authorizationService)
    where TCommand : CartCommand
    where TInputType : IInputObjectGraphType
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    protected CartCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IDistributedLock distributedLock,
        ICartAggregateRepository cartRepository)
        : this(authorizationService, distributedLock, cartRepository)
    {
    }

    protected override TCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);

        // Preserve client-provided UserId (admin/LoginOnBehalf can operate on other users' carts).
        // Fall back to current user when not provided — consistent with PurchaseSchema.GetCartCommand<T>().
        if (string.IsNullOrEmpty(request.UserId))
        {
            request.UserId = context.GetCurrentUserId();
        }

        request.OrganizationId = context.GetCurrentOrganizationId();

        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        var cart = await ResolveCartAsync(request);
        if (cart is not null)
        {
            await Authorize(context, cart.Cart, new CanAccessCartAuthorizationRequirement());
        }
    }

    protected override async Task<CartAggregate> GetResponseAsync(IResolveFieldContext<object> context, TCommand request)
    {
        var lockKey = GetLockKey(request);
        if (string.IsNullOrEmpty(lockKey))
        {
            return await base.GetResponseAsync(context, request);
        }

        await using var handle = await distributedLock.AcquireForGraphQLAsync(lockKey, context);
        return await base.GetResponseAsync(context, request);
    }

    protected override Task AfterMediatorSend(IResolveFieldContext<object> context, TCommand request, CartAggregate response)
    {
        context.SetExpandedObjectGraph(response);

        return base.AfterMediatorSend(context, request, response);
    }

    protected virtual string GetLockKey(TCommand request)
    {
        return !string.IsNullOrEmpty(request.UserId)
            ? $"{PurchaseSchema.CartPrefix}:{request.UserId}"
            : null;
    }

    protected virtual Task<CartAggregate> ResolveCartAsync(TCommand request)
    {
        return !string.IsNullOrEmpty(request.CartId)
            ? cartRepository.GetCartByIdAsync(request.CartId)
            : cartRepository.GetCartAsync(request);
    }
}
