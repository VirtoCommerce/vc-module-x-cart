using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands.BaseCommands;

public abstract class ScopedWishlistCommandHandlerBase<TCommand> : CartCommandHandler<TCommand>
    where TCommand : ScopedWishlistCommand
{
    private readonly ICartSharingService _cartSharingService;

    protected ScopedWishlistCommandHandlerBase(ICartAggregateRepository cartAggregateRepository, ICartSharingService cartSharingService)
        : base(cartAggregateRepository)
    {
        _cartSharingService = cartSharingService;
    }

    protected virtual async Task UpdateScopeAsync(CartAggregate cartAggregate, TCommand request)
    {
        var context = CreateScopeContext(request);

        try
        {
            await _cartSharingService.UpdateScopeAsync(cartAggregate.Cart, context);
        }
        catch
        {
            // A policy may have written part of the scope before validation rejected the rest. The aggregate is
            // cached by reference, so leaving it in place would serve the rejected state to later reads - and the
            // next save would persist it (VCST-6113).
            GenericCachingRegion<CartAggregate>.ExpireTokenForKey(cartAggregate.Id);

            throw;
        }
    }

    protected virtual WishlistScopeContext CreateScopeContext(TCommand request)
    {
        var context = AbstractTypeFactory<WishlistScopeContext>.TryCreateInstance();

        context.Scope = request.Scope;
        context.SharingKey = request.SharingKey;
        context.AddSharedWithIds = request.AddSharedWithIds;
        context.RemoveSharedWithIds = request.RemoveSharedWithIds;
        context.LegacySharedWithId = request.SharedWithId;
        context.Message = request.Message;
        context.CurrentUserId = request.WishlistUserContext.CurrentUserId;
        context.CustomerName = request.WishlistUserContext.CurrentContact.Name;
        context.CurrentOrganizationId = request.WishlistUserContext.CurrentOrganizationId;

        return context;
    }
}
