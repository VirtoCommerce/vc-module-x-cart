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
    public class MoveToSavedForLaterItemsCommandHandler(ICartAggregateRepository cartAggregateRepository, ISavedForLaterListService savedForLaterListService) : IRequestHandler<MoveToSavedForLaterItemsCommand, CartAggregateWithList>
    {
        public async Task<CartAggregateWithList> Handle(MoveToSavedForLaterItemsCommand request, CancellationToken cancellationToken)
        {
            var cart = await GetCartById(request.CartId, request.CultureName);

            if (cart == null)
            {
                throw new OperationCanceledException("Cart not found");
            }

            var saveForLaterList = await savedForLaterListService.EnsureSaveForLaterListAsync(request);

            foreach (var lineItemId in request.LineItemIds)
            {
                var item = cart.Cart.Items.FirstOrDefault(x => x.Id == lineItemId);

                if (item != null)
                {
                    saveForLaterList = await saveForLaterList.AddItemsAsync(new List<NewCartItem> { new NewCartItem(item.ProductId, item.Quantity) });
                    await cart.RemoveItemAsync(lineItemId);
                }
            }

            await SaveCartAsync(saveForLaterList);
            await SaveCartAsync(cart);

            return new CartAggregateWithList() { Cart = cart, List = saveForLaterList };
        }

        protected virtual Task<CartAggregate> GetCartById(string cartId, string language) => cartAggregateRepository.GetCartByIdAsync(cartId, language);

        protected virtual Task SaveCartAsync(CartAggregate cartAggregate) => cartAggregateRepository.SaveAsync(cartAggregate);
    }
}
