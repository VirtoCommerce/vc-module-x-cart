using System.Threading.Tasks;
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

    protected virtual Task UpdateScopeAsync(CartAggregate cartAggregate, TCommand request)
    {
        var context = CreateScopeContext(request);

        return _cartSharingService.UpdateScopeAsync(cartAggregate.Cart, context);
    }

    protected virtual WishlistScopeContext CreateScopeContext(TCommand request)
    {
        var context = AbstractTypeFactory<WishlistScopeContext>.TryCreateInstance();

        context.Scope = request.Scope;
        context.SharingKey = request.SharingKey;
        context.SharedWithId = request.SharedWithId;
        context.CurrentUserId = request.WishlistUserContext.CurrentUserId;
        context.CustomerName = request.WishlistUserContext.CurrentContact.Name;
        context.CurrentOrganizationId = request.WishlistUserContext.CurrentOrganizationId;

        return context;
    }
}
