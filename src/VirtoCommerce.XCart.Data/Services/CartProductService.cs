using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.InventoryModule.Core.Model.Search;
using VirtoCommerce.InventoryModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartProductService : ICartProductService, ICartProductsLoaderService
    {
        private readonly IItemService _productService;
        private readonly IInventorySearchService _inventorySearchService;
        private readonly IPricingEvaluatorService _pricingEvaluatorService;
        private readonly IMapper _mapper;
        private readonly ILoadUserToEvalContextService _loadUserToEvalContextService;
        private readonly IMediator _mediator;

        /// <summary>
        /// Default response group
        /// </summary>
        protected virtual ItemResponseGroup ResponseGroups => ItemResponseGroup.ItemAssets | ItemResponseGroup.ItemInfo | ItemResponseGroup.Outlines | ItemResponseGroup.Seo;
        protected virtual string[] IncludeFields => new string[]
        {
            "__object",
            "price",
            "availabilityData",
            "images",
            "properties",
            "description",
            "slug",
            "outlines"
        };

        /// <summary>
        /// Default page size for pagination
        /// </summary>
        protected virtual int DefaultPageSize => 50;

        public CartProductService(IItemService productService,
            IInventorySearchService inventoryService,
            IPricingEvaluatorService pricingEvaluatorService,
            IMapper mapper,
            ILoadUserToEvalContextService loadUserToEvalContextService,
            IMediator mediator)
        {
            _productService = productService;
            _inventorySearchService = inventoryService;
            _pricingEvaluatorService = pricingEvaluatorService;
            _mapper = mapper;
            _loadUserToEvalContextService = loadUserToEvalContextService;
            _mediator = mediator;
        }

        /// <summary>
        /// Load <see cref="CartProduct"/>s with all dependencies
        /// </summary>
        /// <param name="aggregate">Cart aggregate</param>
        /// <param name="ids">Product ids</param>
        /// <returns>List of <see cref="CartProduct"/>s</returns>
        public async Task<IList<CartProduct>> GetCartProductsByIdsAsync(CartAggregate aggregate, IList<string> ids)
        {
            if (aggregate is null || ids.IsNullOrEmpty())
            {
                return [];
            }

            var cartProducts = await GetCartProductsAsync(ids, aggregate.Store.Id, aggregate.Cart.Currency, aggregate.Cart.CustomerId, aggregate.ProductsIncludeFields ?? IncludeFields);

            var productsToLoadDependencies = cartProducts.Where(x => x.LoadDependencies).ToList();
            if (productsToLoadDependencies.Count != 0)
            {
                await Task.WhenAll(LoadDependencies(aggregate, productsToLoadDependencies));
            }

            return cartProducts;
        }

        /// <summary>
        /// Load <see cref="CartProduct"/>s with all dependencies
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>List of <see cref="CartProduct"/>s</returns>
        public async Task<IList<CartProduct>> GetCartProductsAsync(CartProductsRequest request)
        {
            if (request is null || request.ProductIds.IsNullOrEmpty())
            {
                return [];
            }

            var cartProducts = await LoadCartProductsAsync(request.ProductIds, request.Store.Id, request.Currency.Code, request.UserId, request.ProductsIncludeFields ?? IncludeFields, request.EvaluatePromotions);

            var productsToLoadDependencies = cartProducts.Where(x => x.LoadDependencies).ToList();
            if (productsToLoadDependencies.Count != 0)
            {
                await Task.WhenAll(LoadDependencies(request, productsToLoadDependencies));
            }

            return cartProducts;
        }

        /// <summary>
        /// Load <see cref="CatalogProduct"/>s
        /// </summary>
        /// <param name="ids">Product ids</param>
        /// <returns>List of <see cref="CatalogProduct"/>s</returns>
        protected virtual async Task<IList<CatalogProduct>> GetProductsByIdsAsync(IList<string> ids)
        {
            return await _productService.GetAsync(ids, ResponseGroups.ToString());
        }


        /// <summary>
        /// Map all <see cref="CatalogProduct"/> to <see cref="CartProduct"/>
        /// </summary>
        /// <returns>List of <see cref="CartProduct"/>s</returns>
        protected virtual async Task<List<CartProduct>> GetCartProductsAsync(IList<string> ids, string storeId, string currencyCode, string userId, IList<string> includeFields)
        {
            // EvaluatePromotions = false - Promotions will be applied on the line item level
            var result = await LoadCartProductsAsync(ids, storeId, currencyCode, userId, includeFields, false);
            return result;
        }

        protected virtual async Task<List<CartProduct>> LoadCartProductsAsync(IList<string> ids, string storeId, string currencyCode, string userId, IList<string> includeFields, bool evaluatePromotions)
        {
            var productsQuery = new LoadProductsQuery
            {
                UserId = userId,
                StoreId = storeId,
                CurrencyCode = currencyCode,
                ObjectIds = ids,
                IncludeFields = includeFields,
                EvaluatePromotions = evaluatePromotions,
            };

            var response = await _mediator.Send(productsQuery);
            var result = response.Products.Select(x => new CartProduct(x)).ToList();
            return result;
        }

        /// <summary>
        /// Load all properties for <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="aggregate">Cart aggregate</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        /// <returns>List of <see cref="Task"/>s</returns>
        protected virtual List<Task> LoadDependencies(CartAggregate aggregate, List<CartProduct> products) => new List<Task>
        {
            ApplyInventoriesToCartProductAsync(aggregate, products),
            ApplyPricesToCartProductAsync(aggregate, products)
        };

        /// <summary>
        /// Load all properties for <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        /// <returns>List of <see cref="Task"/>s</returns>
        protected virtual List<Task> LoadDependencies(CartProductsRequest request, List<CartProduct> products)
        {
            var result = new List<Task>();

            if (request.LoadInventory)
            {
                result.Add(ApplyInventoriesToCartProductAsync(request, products));
            }

            if (request.LoadPrice)
            {
                result.Add(ApplyPricesToCartProductAsync(request, products));
            }

            return result;
        }

        /// <summary>
        /// Load inventories and apply them to <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="aggregate">Cart aggregate</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        protected virtual async Task ApplyInventoriesToCartProductAsync(CartAggregate aggregate, List<CartProduct> products)
        {
            if (products.IsNullOrEmpty())
            {
                return;
            }

            var ids = products.Select(x => x.Id).ToArray();

            var countResult = await _inventorySearchService.SearchInventoriesAsync(new InventorySearchCriteria
            {
                ProductIds = ids,
                Skip = 0,
                Take = DefaultPageSize
            });

            var allLoadInventories = countResult.Results.ToList();

            if (countResult.TotalCount > DefaultPageSize)
            {
                for (var i = DefaultPageSize; i < countResult.TotalCount; i += DefaultPageSize)
                {
                    var loadInventoriesTask = await _inventorySearchService.SearchInventoriesAsync(new InventorySearchCriteria
                    {
                        ProductIds = ids,
                        Skip = i,
                        Take = DefaultPageSize
                    });

                    allLoadInventories.AddRange(loadInventoriesTask.Results);
                }
            }

            foreach (var cartProduct in products)
            {
                cartProduct.ApplyInventories(allLoadInventories, aggregate.Store);
            }
        }

        /// <summary>
        /// Load inventories and apply them to <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        protected virtual async Task ApplyInventoriesToCartProductAsync(CartProductsRequest request, List<CartProduct> products)
        {
            if (products.IsNullOrEmpty())
            {
                return;
            }

            var ids = products.Select(x => x.Id).ToArray();

            var countResult = await _inventorySearchService.SearchInventoriesAsync(new InventorySearchCriteria
            {
                ProductIds = ids,
                Skip = 0,
                Take = DefaultPageSize
            });

            var allLoadInventories = countResult.Results.ToList();

            if (countResult.TotalCount > DefaultPageSize)
            {
                for (var i = DefaultPageSize; i < countResult.TotalCount; i += DefaultPageSize)
                {
                    var loadInventoriesTask = await _inventorySearchService.SearchInventoriesAsync(new InventorySearchCriteria
                    {
                        ProductIds = ids,
                        Skip = i,
                        Take = DefaultPageSize
                    });

                    allLoadInventories.AddRange(loadInventoriesTask.Results);
                }
            }

            foreach (var cartProduct in products)
            {
                cartProduct.ApplyInventories(allLoadInventories, request.Store);
            }
        }

        /// <summary>
        /// Evaluate prices and apply them to <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="aggregate">Cart aggregate</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        protected virtual async Task ApplyPricesToCartProductAsync(CartAggregate aggregate, List<CartProduct> products)
        {
            if (aggregate is null || products.IsNullOrEmpty())
            {
                return;
            }

            var pricesEvalContext = _mapper.Map<PriceEvaluationContext>(aggregate);
            pricesEvalContext.ProductIds = products.Select(x => x.Id).ToArray();

            // There was a call to pipeline execution and stack overflow comes as a result of infinite cart getting,
            // because the LoadCartToEvalContextMiddleware catches pipeline execution.
            // Replaced to direct mapping.
            _mapper.Map(aggregate, pricesEvalContext);

            await _loadUserToEvalContextService.SetShopperDataFromMember(pricesEvalContext, pricesEvalContext.CustomerId);

            var evalPricesTask = await _pricingEvaluatorService.EvaluateProductPricesAsync(pricesEvalContext);

            foreach (var cartProduct in products)
            {
                cartProduct.ApplyPrices(evalPricesTask, aggregate.Currency);
            }
        }

        /// <summary>
        /// Evaluate prices and apply them to <see cref="CartProduct"/>s
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="products">List of <see cref="CartProduct"/>s</param>
        protected virtual async Task ApplyPricesToCartProductAsync(CartProductsRequest request, List<CartProduct> products)
        {
            if (request is null || products.IsNullOrEmpty())
            {
                return;
            }

            var pricesEvalContext = _mapper.Map<PriceEvaluationContext>(request);
            pricesEvalContext.ProductIds = products.Select(x => x.Id).ToArray();

            // There was a call to pipeline execution and stack overflow comes as a result of infinite cart getting,
            // because the LoadCartToEvalContextMiddleware catches pipeline execution.
            // Replaced to direct mapping.
            _mapper.Map(request, pricesEvalContext);

            await _loadUserToEvalContextService.SetShopperDataFromMember(pricesEvalContext, pricesEvalContext.CustomerId);

            var evalPricesTask = await _pricingEvaluatorService.EvaluateProductPricesAsync(pricesEvalContext);

            foreach (var cartProduct in products)
            {
                cartProduct.ApplyPrices(evalPricesTask, request.Currency);
            }
        }
    }
}
