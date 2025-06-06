using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddWishlistBulkItemCommandHandler : IRequestHandler<AddWishlistBulkItemCommand, BulkCartAggregateResult>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;

        public AddWishlistBulkItemCommandHandler(ICartAggregateRepository cartAggregateRepository)
        {
            _cartAggregateRepository = cartAggregateRepository;
        }

        public virtual async Task<BulkCartAggregateResult> Handle(AddWishlistBulkItemCommand request, CancellationToken cancellationToken)
        {
            var result = new BulkCartAggregateResult();

            foreach (var listId in request.ListIds)
            {
                var cartAggregate = await _cartAggregateRepository.GetCartByIdAsync(listId);

                cartAggregate.ValidationRuleSet = ["default"];
                await cartAggregate.AddItemsAsync(new List<NewCartItem> {
                    new NewCartItem(request.ProductId, request.Quantity ?? 1)
                    {
                        IsWishlist = true,
                    }
                });

                await _cartAggregateRepository.SaveAsync(cartAggregate);

                result.CartAggregates.Add(cartAggregate);
            }

            return result;
        }
    }
}
