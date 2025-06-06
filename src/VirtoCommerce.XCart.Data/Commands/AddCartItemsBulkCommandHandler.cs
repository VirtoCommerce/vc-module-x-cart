using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddCartItemsBulkCommandHandler : IRequestHandler<AddCartItemsBulkCommand, BulkCartResult>
    {
        private readonly IProductIndexedSearchService _productIndexedSearchService;
        private readonly IStoreService _storeService;
        private readonly IMediator _mediator;

        public AddCartItemsBulkCommandHandler(
            IProductIndexedSearchService productIndexedSearchService,
            IStoreService storeService,
            IMediator mediator)
        {
            _productIndexedSearchService = productIndexedSearchService;
            _storeService = storeService;
            _mediator = mediator;
        }

        public virtual async Task<BulkCartResult> Handle(AddCartItemsBulkCommand request, CancellationToken cancellationToken)
        {
            var result = new BulkCartResult();
            var cartItemsToAdd = new List<NewCartItem>();
            var requestedItems = request.CartItems.ToList();

            // find products by skus
            var products = await FindProductsBySkuAsync(request);

            // check for duplicates
            var duplicates = GetDuplicatesBySku(products);
            if (duplicates.Count > 0)
            {
                foreach (var duplicate in duplicates)
                {
                    var error = CartErrorDescriber.ProductDuplicateError(nameof(CatalogProduct), duplicate.Key, duplicate.Value);
                    result.Errors.Add(error);
                }

                // remove duplicates from requested items
                requestedItems = requestedItems.Where(x => !duplicates.ContainsKey(x.ProductSku)).ToList();
            }

            foreach (var item in requestedItems)
            {
                var product = products.FirstOrDefault(x => x.Code == item.ProductSku);
                if (product != null)
                {
                    var newCartItem = new NewCartItem(product.Id, item.Quantity);
                    cartItemsToAdd.Add(newCartItem);
                }
                else
                {
                    var error = CartErrorDescriber.ProductInvalidError(nameof(CatalogProduct), item.ProductSku);
                    result.Errors.Add(error);
                }
            }

            // send Add to Cart command
            var command = AbstractTypeFactory<AddCartItemsCommand>.TryCreateInstance();
            command.CopyFrom(request);
            command.CartId = request.CartId;
            command.CartItems = cartItemsToAdd.ToArray();

            var cartAggregate = await _mediator.Send(command, cancellationToken);

            result.Cart = cartAggregate;

            var lineItemErrors = cartAggregate.GetValidationErrors()
                .OfType<CartValidationError>()
                .Where(x => x.ObjectType == nameof(CatalogProduct));

            // update validation errors with product skus
            UpdateValidationErrors(lineItemErrors, products);

            result.Errors.AddRange(lineItemErrors);

            return result;
        }

        protected virtual async Task<IList<CatalogProduct>> FindProductsBySkuAsync(AddCartItemsBulkCommand request)
        {
            var productSkus = request.CartItems.Select(x => x.ProductSku).ToList();

            long totalCount;
            var result = new List<CatalogProduct>();

            var indexedSearchCriteria = new ProductIndexedSearchCriteria
            {
                StoreId = request.StoreId,
                CatalogId = await GetCatalogId(request.StoreId),
                Terms = [new TermFilter { FieldName = "code", Values = productSkus }.ToString()],
                SearchInVariations = true,
                ResponseGroup = ItemResponseGroup.ItemInfo.ToString(),
                Take = 20
            };

            do
            {
                var searchResult = await _productIndexedSearchService.SearchAsync(indexedSearchCriteria);
                result.AddRange(searchResult.Items);

                totalCount = searchResult.TotalCount;
                indexedSearchCriteria.Skip += indexedSearchCriteria.Take;
            }
            while (indexedSearchCriteria.Skip < totalCount);

            return result;
        }

        private async Task<string> GetCatalogId(string storeId)
        {
            var store = await _storeService.GetByIdAsync(storeId, StoreResponseGroup.StoreInfo.ToString(), false);
            return store.Catalog;
        }

        protected virtual IDictionary<string, List<string>> GetDuplicatesBySku(IList<CatalogProduct> catalogProducts)
        {
            var productIdsBySku = new Dictionary<string, List<string>>();

            foreach (var product in catalogProducts)
            {
                if (productIdsBySku.TryGetValue(product.Code, out var value))
                {
                    value.Add(product.Id);
                }
                else
                {
                    productIdsBySku.Add(product.Code, [product.Id]);
                }
            }

            return productIdsBySku
                .Where(x => x.Value.Count > 1)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected virtual void UpdateValidationErrors(IEnumerable<CartValidationError> lineItemErrors, IList<CatalogProduct> products)
        {
            foreach (var error in lineItemErrors)
            {
                var product = products.FirstOrDefault(x => x.Id == error.ObjectId);
                if (product != null)
                {
                    error.ObjectId = product.Code;
                }
            }
        }
    }
}
