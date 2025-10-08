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
            var requestItems = CombineRequestItems(request);
            var nonZeroQuantityProductIds = requestItems
                .Where(x => x.Quantity > 0)
                .Select(x => x.ProductId)
                .ToArray();

            var productsTask = LoadCartProductsAsync(request, nonZeroQuantityProductIds);
            var cartAggregateTask = GetOrCreateCartFromCommandAsync(request);
            await Task.WhenAll(productsTask, cartAggregateTask);

            var cartAggregate = cartAggregateTask.Result;
            var products = productsTask.Result;

            foreach (var requestItem in requestItems.Where(x => x.Quantity == 0))
            {
                await cartAggregate.RemoveItemsByProductIdAsync(requestItem.ProductId);
            }

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
                        OverrideQuantity = true,
                    });
                }
            }

            foreach (var item in newCartItems)
            {
                await cartAggregate.AddItemAsync(item);
            }

            return await SaveCartAsync(cartAggregate);
        }

        private async Task<IList<CartProduct>> LoadCartProductsAsync(UpdateCartQuantityCommand request, string[] productIds)
        {
            var productRequest = new CartProductsRequest
            {
                LoadPrice = false,
                LoadInventory = false,
                EvaluatePromotions = false,

                CultureName = request.CultureName,
                StoreId = request.StoreId,
                CurrencyCode = request.CurrencyCode,

                ProductIds = productIds,
                ProductsIncludeFields = ["id", "name", "code"],
            };

            var products = await _cartProductsLoaderService.GetCartProductsAsync(productRequest);
            return products;
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
