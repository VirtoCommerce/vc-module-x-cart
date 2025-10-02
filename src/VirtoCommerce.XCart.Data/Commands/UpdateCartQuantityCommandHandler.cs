using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateCartQuantityCommandHandler : CartCommandHandler<UpdateCartQuantityCommand>
    {
        private readonly ICartProductsLoaderService _cartProductsLoaderService;
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;
        private readonly IMediator _mediator;

        public UpdateCartQuantityCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductsLoaderService cartProductsLoaderService,
            IProductConfigurationSearchService productConfigurationSearchService,
            IMediator mediator
            )
            : base(cartAggregateRepository)
        {
            _cartProductsLoaderService = cartProductsLoaderService;
            _productConfigurationSearchService = productConfigurationSearchService;
            _mediator = mediator;
        }

        protected override Task<CartAggregate> GetCartById(string cartId, string language)
            => CartRepository.GetCartByIdAsync(cartId, ["id", "name", "code"], language);

        protected override Task<CartAggregate> GetCart(ShoppingCartSearchCriteria cartSearchCriteria, string language)
            => CartRepository.GetCartAsync(cartSearchCriteria, ["id", "name", "code"], language);

        public override async Task<CartAggregate> Handle(UpdateCartQuantityCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var requestItems = CombineRequestItems(request);
            foreach (var requestItem in requestItems.Where(x => x.Quantity == 0))
            {
                await cartAggregate.RemoveItemsByProductIdAsync(requestItem.ProductId);
                requestItems.Remove(requestItem);
            }

            var productIds = requestItems
                .Select(x => x.ProductId)
                .ToArray();

            var productRequest = new CartProductsRequest
            {
                EvaluatePromotions = false,
                LoadInventory = false,
                LoadPrice = false,

                CultureName = request.CultureName,
                Store = cartAggregate.Store,
                Currency = cartAggregate.Currency,

                ProductIds = productIds,

                ProductsIncludeFields = ["id", "name", "code"],
            };

            var products = await _cartProductsLoaderService.GetCartProductsAsync(productRequest);

            var newCartItems = new List<NewCartItem>();
            foreach (var product in products)
            {
                var requestItem = requestItems.FirstOrDefault(x => x.ProductId == product.Id);
                if (requestItem != null)
                {
                    newCartItems.Add(new NewCartItem(product.Id, requestItem.Quantity)
                    {
                        CartProduct = product,
                        IgnoreValidationErrors = true,
                    });
                }

            }

            foreach (var item in newCartItems)
            {
                await cartAggregate.AddItemAsync(item);
            }

            return await SaveCartAsync(cartAggregate);
        }

        private static List<UpdateCartQuantityItem> CombineRequestItems(UpdateCartQuantityCommand request)
        {
            var result = new List<UpdateCartQuantityItem>();

            foreach (var item in request.Items)
            {
                var a = result.FirstOrDefault(x => x.ProductId == item.ProductId);
                if (a != null)
                {
                    a.Quantity += item.Quantity;
                }
                else
                {
                    result.Add(new UpdateCartQuantityItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
                }
            }

            return result;
        }
    }
}
