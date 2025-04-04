using System;
using System.Linq;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCatalog.Core.Models;

namespace VirtoCommerce.XCart.Data.Middlewares
{
    public class EvalProductsWishlistsMiddleware : IAsyncMiddleware<SearchProductResponse>
    {
        private readonly IWishlistService _wishlistService;

        public EvalProductsWishlistsMiddleware(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        public async Task Run(SearchProductResponse parameter, Func<SearchProductResponse, Task> next)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            var query = parameter.Query;
            if (query == null)
            {
                throw new OperationCanceledException("Query must be set");
            }

            var productIds = parameter.Results.Select(x => x.Id).ToArray();
            var responseGroup = EnumUtility.SafeParse(query.GetResponseGroup(), ExpProductResponseGroup.None);
            // If products availabilities requested
            if (responseGroup.HasFlag(ExpProductResponseGroup.LoadWishlists) &&
                productIds.Length != 0)
            {
                var wishlistsByProducts = await _wishlistService.FindWishlistsByProductsAsync(query.UserId, query.OrganizationId, query.StoreId, productIds);

                if (wishlistsByProducts.Any())
                {
                    parameter.Results.Apply((item) =>
                    {
                        if (wishlistsByProducts.TryGetValue(item.Id, out var wishlistIds))
                        {
                            item.WishlistIds = wishlistIds;
                        }
                        item.InWishlist = item.WishlistIds.Any();
                    });
                }
            }

            await next(parameter);
        }
    }
}
