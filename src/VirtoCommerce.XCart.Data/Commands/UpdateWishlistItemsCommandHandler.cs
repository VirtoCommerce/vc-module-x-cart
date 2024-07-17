using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateWishlistItemsCommandHandler : CartCommandHandler<UpdateWishlistItemsCommand>
    {
        private readonly ICartProductService _cartProductService;

        public UpdateWishlistItemsCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartProductService cartProductService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
        }

        public override async Task<CartAggregate> Handle(UpdateWishlistItemsCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await CartRepository.GetCartByIdAsync(request.ListId);

            cartAggregate.ValidationRuleSet = ["default"];

            foreach (var item in request.Items)
            {
                var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id.Equals(item.LineItemId));
                if (lineItem != null)
                {
                    var product = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, new[] { lineItem.ProductId })).FirstOrDefault();

                    await cartAggregate.ChangeItemQuantityAsync(new ItemQtyAdjustment
                    {
                        LineItem = lineItem,
                        LineItemId = item.LineItemId,
                        NewQuantity = item.Quantity,
                        CartProduct = product
                    });
                }
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
