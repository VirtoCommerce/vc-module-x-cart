using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands;

public class CloneWishlistCommandHandler : ScopedWishlistCommandHandlerBase<CloneWishlistCommand>
{
    private readonly IShoppingCartService _shoppingCartService;

    public CloneWishlistCommandHandler(ICartAggregateRepository cartAggregateRepository, IShoppingCartService shoppingCartService)
        : base(cartAggregateRepository)
    {
        _shoppingCartService = shoppingCartService;
    }

    public override async Task<CartAggregate> Handle(CloneWishlistCommand request, CancellationToken cancellationToken)
    {
        request.CartType = ListTypeName;

        var cloneCartAggregate = await CreateNewCartAggregateAsync(request);
        cloneCartAggregate.Cart.Description = request.Description;
        await UpdateScopeAsync(cloneCartAggregate, request);

        var cart = request.WishlistUserContext.Cart ?? await _shoppingCartService.GetByIdAsync(request.ListId);
        if (cart != null)
        {
            cloneCartAggregate.ValidationRuleSet = ["default"];

            var items = cart.Items
                            ?.Select(x => new NewCartItem(x.ProductId, x.Quantity) { IsWishlist = true })
                            .ToArray()
                        ?? [];

            await cloneCartAggregate.AddItemsAsync(items);
        }

        return await SaveCartAsync(cloneCartAggregate);
    }
}
