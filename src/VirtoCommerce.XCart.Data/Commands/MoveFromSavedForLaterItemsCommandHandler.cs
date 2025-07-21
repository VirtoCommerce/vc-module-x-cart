using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class MoveFromSavedForLaterItemsCommandHandler(ICartAggregateRepository cartAggregateRepository, ISavedForLaterListService savedForLaterListService) : IRequestHandler<MoveFromSavedForLaterItemsCommand, CartAggregateWithList>
    {
        public async Task<CartAggregateWithList> Handle(MoveFromSavedForLaterItemsCommand request, CancellationToken cancellationToken)
        {
            var cart = await GetCartById(request.CartId, request.CultureName);

            if (cart == null)
            {
                throw new OperationCanceledException("Cart not found");
            }

            var savedForLaterList = await savedForLaterListService.FindSavedForLaterListAsync(request);

            if (savedForLaterList == null)
            {
                throw new OperationCanceledException("Saved for later not found");
            }

            foreach (var lineItemId in request.LineItemIds)
            {
                var item = savedForLaterList.Cart.Items.FirstOrDefault(x => x.Id == lineItemId);

                if (item != null)
                {
                    cart = await cart.AddItemsAsync(new List<NewCartItem> { new NewCartItem(item.ProductId, item.Quantity) });
                    await savedForLaterList.RemoveItemAsync(lineItemId);
                }
            }

            await SaveCartAsync(savedForLaterList);
            await SaveCartAsync(cart);

            return new CartAggregateWithList() { Cart = cart, List = savedForLaterList };
        }

        protected virtual Task<CartAggregate> GetCartById(string cartId, string language) => cartAggregateRepository.GetCartByIdAsync(cartId, language);

        protected virtual Task SaveCartAsync(CartAggregate cartAggregate) => cartAggregateRepository.SaveAsync(cartAggregate);
    }
}
