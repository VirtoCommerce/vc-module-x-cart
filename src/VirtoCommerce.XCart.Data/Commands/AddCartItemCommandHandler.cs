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
    public class AddCartItemCommandHandler : CartCommandHandler<AddCartItemCommand>
    {
        private readonly ICartProductService _cartProductService;

        public AddCartItemCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartProductService cartProductService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
        }

        public override async Task<CartAggregate> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            var product = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, new[] { request.ProductId })).FirstOrDefault();
            await cartAggregate.AddItemAsync(new NewCartItem(request.ProductId, request.Quantity)
            {
                Comment = request.Comment,
                DynamicProperties = request.DynamicProperties,
                Price = request.Price,
                CartProduct = product
            });

            return await SaveCartAsync(cartAggregate);
        }
    }
}
