using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
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

        public UpdateCartQuantityCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductsLoaderService cartProductsLoaderService
            )
            : base(cartAggregateRepository)
        {
            _cartProductsLoaderService = cartProductsLoaderService;
        }

        protected override Task<CartAggregate> GetCartById(string cartId, string language)
            => CartRepository.GetCartByIdAsync(cartId, ["__none"], language);

        protected override Task<CartAggregate> GetCart(ShoppingCartSearchCriteria cartSearchCriteria, string language)
            => CartRepository.GetCartAsync(cartSearchCriteria, ["__none"], language);

        public override async Task<CartAggregate> Handle(UpdateCartQuantityCommand request, CancellationToken cancellationToken)
        {
            var requestItems = CombineRequestItems(request);

            var requestItemsByCurrency = requestItems
                .GroupBy(x => x.CurrencyCode)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var productTasks = new List<Task>();
            foreach (var currencyRequestItemsPair in requestItemsByCurrency)
            {
                var nonZeroQuantityProductIdsByCurrency = currencyRequestItemsPair.Value
                    .Where(x => x.Quantity > 0)
                    .Select(x => x.ProductId)
                    .ToArray();

                var currencyRequest = (UpdateCartQuantityCommand)request.Clone();
                currencyRequest.CurrencyCode = currencyRequestItemsPair.Key;
                var currencyProductTask = LoadCartProductsAsync(currencyRequest, nonZeroQuantityProductIdsByCurrency);

                productTasks.Add(currencyProductTask);
            }

            var cartAggregateTask = GetOrCreateCartFromCommandAsync(request);

            var allTasks = new List<Task>(productTasks) { cartAggregateTask };
            await Task.WhenAll(allTasks);

            var cartAggregate = cartAggregateTask.Result;

            foreach (var requestItem in requestItems.Where(x => x.Quantity == 0))
            {
                await cartAggregate.RemoveItemsByProductIdAndCurrencyAsync(requestItem.ProductId, requestItem.CurrencyCode);
            }

            var newCartItems = new List<NewCartItem>();

            foreach (var task in productTasks.OfType<Task<ProductsByCurrencyResult>>())
            {
                var productsByCurrency = task.Result;

                foreach (var product in productsByCurrency.Products)
                {
                    var requestItem = requestItems.FirstOrDefault(x => x.ProductId == product.Id && x.CurrencyCode.EqualsIgnoreCase(productsByCurrency.CurrencyCode));
                    if (requestItem != null)
                    {
                        newCartItems.Add(new NewCartItem(product.Id, requestItem.Quantity)
                        {
                            CartProduct = product,
                            CurrencyCode = productsByCurrency.CurrencyCode,
                            IgnoreValidationErrors = true,
                            OverrideQuantity = true,
                        });
                    }
                }
            }

            foreach (var item in newCartItems)
            {
                await cartAggregate.AddItemAsync(item);
            }

            return await SaveCartAsync(cartAggregate);
        }

        protected virtual async Task<ProductsByCurrencyResult> LoadCartProductsAsync(UpdateCartQuantityCommand request, string[] productIds)
        {
            var productRequest = GetCartProductsRequest(request, productIds);

            var products = await _cartProductsLoaderService.GetCartProductsAsync(productRequest);

            return new ProductsByCurrencyResult
            {
                CurrencyCode = productRequest.CurrencyCode,
                Products = products,
            };
        }

        protected virtual CartProductsRequest GetCartProductsRequest(UpdateCartQuantityCommand request, string[] productIds)
        {
            var productRequest = AbstractTypeFactory<CartProductsRequest>.TryCreateInstance();

            productRequest.LoadPrice = false;
            productRequest.LoadInventory = false;
            productRequest.EvaluatePromotions = false;

            productRequest.StoreId = request.StoreId;
            productRequest.UserId = request.UserId;
            productRequest.OrganizationId = request.OrganizationId;
            productRequest.CultureName = request.CultureName;
            productRequest.CurrencyCode = request.CurrencyCode;

            productRequest.ProductIds = productIds;
            productRequest.ProductsIncludeFields = ["id", "name", "code"];

            return productRequest;
        }

        protected static List<UpdateCartQuantityItem> CombineRequestItems(UpdateCartQuantityCommand request)
        {
            var result = new List<UpdateCartQuantityItem>();

            foreach (var item in request.Items)
            {
                if (item.CurrencyCode.IsNullOrEmpty())
                {
                    item.CurrencyCode = request.CurrencyCode;
                }

                var resultItem = result.FirstOrDefault(x => x.ProductId == item.ProductId && x.CurrencyCode.EqualsIgnoreCase(item.CurrencyCode));
                if (resultItem != null)
                {
                    resultItem.Quantity += item.Quantity;
                }
                else
                {
                    result.Add(new UpdateCartQuantityItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        CurrencyCode = item.CurrencyCode,
                    });
                }
            }

            return result;
        }

        public class ProductsByCurrencyResult
        {
            public string CurrencyCode { get; set; }

            public IList<CartProduct> Products { get; set; }
        }
    }
}
