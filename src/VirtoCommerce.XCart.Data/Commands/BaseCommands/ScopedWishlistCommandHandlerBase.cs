using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
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
        if (request.Scope?.EqualsIgnoreCase(CartSharingScope.AnyoneAnonymous) == true)
        {
            _cartSharingService.EnsureSharingSettings(cartAggregate.Cart, request.SharingKey, CartSharingScope.AnyoneAnonymous, CartSharingAccess.Read);
            _cartSharingService.SetOwner(cartAggregate.Cart, request.WishlistUserContext.CurrentUserId, request.WishlistUserContext.CurrentContact.Name, null);
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Organization) == true)
        {
            _cartSharingService.EnsureSharingSettings(cartAggregate.Cart, request.SharingKey, CartSharingScope.Organization, CartSharingAccess.Write);
            _cartSharingService.SetOwner(cartAggregate.Cart, request.WishlistUserContext.CurrentUserId, request.WishlistUserContext.CurrentContact.Name, request.WishlistUserContext.CurrentOrganizationId);
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Private) == true)
        {
            _cartSharingService.EnsureSharingSettings(cartAggregate.Cart, null, CartSharingScope.Private, CartSharingAccess.Write);
            _cartSharingService.SetOwner(cartAggregate.Cart, request.WishlistUserContext.CurrentUserId, request.WishlistUserContext.CurrentContact.Name, null);
        }

        return Task.CompletedTask;
    }
}
