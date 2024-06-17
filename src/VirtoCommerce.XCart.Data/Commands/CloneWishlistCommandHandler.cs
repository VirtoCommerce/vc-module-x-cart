using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CloneWishlistCommandHandler : CartCommandHandler<CloneWishlistCommand>
{
    private readonly IMemberResolver _memberResolver;
    private readonly IShoppingCartService _shoppingCartService;

    public CloneWishlistCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        IMemberResolver memberResolver,
        IShoppingCartService shoppingCartService)
        : base(cartAggregateRepository)
    {
        _memberResolver = memberResolver;
        _shoppingCartService = shoppingCartService;
    }

    public override async Task<CartAggregate> Handle(CloneWishlistCommand request, CancellationToken cancellationToken)
    {
        request.CartType = ModuleConstants.ListTypeName;
        var cloneCartAggregate = await CreateNewCartAggregateAsync(request);

        var contact = request.WishlistUserContext.CurrentContact
                      ?? await _memberResolver.ResolveMemberByIdAsync(request.UserId) as Contact;

        cloneCartAggregate.Cart.Description = request.Description;
        cloneCartAggregate.Cart.CustomerName = contact?.Name;

        if (request.Scope?.EqualsInvariant(ModuleConstants.OrganizationScope) == true)
        {
            var organizationId = contact?.Organizations?.FirstOrDefault();

            cloneCartAggregate.Cart.OrganizationId = organizationId;
        }

        var cart = request.WishlistUserContext.Cart
                   ?? await _shoppingCartService.GetByIdAsync(request.ListId);

        if (cart != null)
        {
            cloneCartAggregate.ValidationRuleSet = ["default"];

            var items = cart.Items?
                            .Select(x => new NewCartItem(x.ProductId, x.Quantity) { IsWishlist = true })
                            .ToArray()
                        ?? [];

            await cloneCartAggregate.AddItemsAsync(items);
        }

        return await SaveCartAsync(cloneCartAggregate);
    }
}
