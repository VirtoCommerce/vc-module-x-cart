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
    public class ChangeCartItemCommentCommandHandler : CartCommandHandler<ChangeCartItemCommentCommand>
    {
        private readonly ICartProductService _cartProductService;

        public ChangeCartItemCommentCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartProductService cartProductService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
        }

        public override async Task<CartAggregate> Handle(ChangeCartItemCommentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id.Equals(request.LineItemId));

            if (lineItem != null &&
                (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, new[] { lineItem.ProductId })).FirstOrDefault() == null)
            {
                return cartAggregate;
            }

            await cartAggregate.ChangeItemCommentAsync(new NewItemComment(request.LineItemId, request.Comment));

            return await SaveCartAsync(cartAggregate);
        }
    }
}
