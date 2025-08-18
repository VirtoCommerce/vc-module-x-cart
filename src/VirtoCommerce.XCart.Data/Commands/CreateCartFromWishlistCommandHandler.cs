using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateCartFromWishlistCommandHandler : CartCommandHandler<CreateCartFromWishlistCommand>
{
    public CreateCartFromWishlistCommandHandler(ICartAggregateRepository cartAggregateRepository) : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(CreateCartFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var sourceAggreagate = request.WishlistUserContext.Cart == null
            ? await CartRepository.GetCartByIdAsync(request.ListId)
            : await CartRepository.GetCartForShoppingCartAsync(request.WishlistUserContext.Cart);

        var secondaryCartCommand = new CreateCartFromWishlistCommand
        {
            StoreId = sourceAggreagate.Cart.StoreId,
            CartName = Guid.NewGuid().ToString("N"),
            CartType = "CreatedFromWishlist",
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            CultureName = sourceAggreagate.Cart.LanguageCode,
            CurrencyCode = sourceAggreagate.Currency.Code,
        };

        var secondaryCartAggregate = await GetOrCreateCartFromCommandAsync(secondaryCartCommand);

        await CopyItems(sourceAggreagate, secondaryCartAggregate);

        await CartRepository.SaveAsync(secondaryCartAggregate);
        return secondaryCartAggregate;
    }

    protected virtual async Task CopyItems(CartAggregate sourceAggregate, CartAggregate destinationCartAggregate)
    {
        var ordinaryItems = sourceAggregate.LineItems
            .Where(x => !x.IsConfigured)
            .ToArray();

        if (ordinaryItems.Length > 0)
        {
            var newCartItems = ordinaryItems
                .Select(x => new NewCartItem(x.ProductId, x.Quantity)
                {
                    IgnoreValidationErrors = true,
                    IsSelectedForCheckout = true,
                    CreatedDate = x.CreatedDate,
                    Comment = x.Note,
                })
                .ToArray();

            await destinationCartAggregate.AddItemsAsync(newCartItems);
        }
    }
}
