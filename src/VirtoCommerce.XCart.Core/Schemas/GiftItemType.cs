using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class GiftItemType : ExtendableGraphType<GiftItem>
    {
        public GiftItemType(IMediator mediator, IDataLoaderContextAccessor dataLoader, IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field(x => x.PromotionId).Description("Promotion ID");
            Field(x => x.Quantity).Description("Number of gifts in the reward");
            Field(x => x.ProductId, true).Description("Product ID");
            Field(x => x.CategoryId, true).Description("Product category ID");
            Field(x => x.ImageUrl, true).Description("Value of reward image absolute URL");
            Field(x => x.Name).Description("Name of the reward");
            Field(x => x.MeasureUnit, true).Description("Measurement unit");
            Field(x => x.LineItemId, true).Description("Line item ID in case there is a gift in the cart. If there is no gift, it stays null");

            AddField(new FieldType
            {
                Name = "id",
                Description = "Artificial ID for this value object",
                Type = GraphTypeExtensionHelper.GetActualType<NonNullGraphType<StringGraphType>>(),
                Resolver = new FuncFieldResolver<GiftItem, string>(context =>
                {
                    // CacheKey as Id. CacheKey is determined by the values returned form GetEqualityComponents().
                    return context.Source.GetCacheKey();
                })
            });

            AddField(new FieldType
            {
                Name = "product",
                Type = GraphTypeExtensionHelper.GetActualType<ProductType>(),
                Resolver = new FuncFieldResolver<GiftItem, IDataLoaderResult<ExpProduct>>(context =>
                {
                    if (context.Source.ProductId.IsNullOrEmpty())
                    {
                        return default;
                    }

                    var includeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
                    var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>("cart_gifts_products", async (ids) =>
                    {
                        //Gift is not part of cart, can't use CartAggregate. Getting store and currency from the context.
                        var request = new LoadProductsQuery
                        {
                            UserId = context.GetArgumentOrValue<string>("userId") ?? context.GetCurrentUserId(),
                            StoreId = context.GetValue<string>("storeId"),
                            CurrencyCode = context.GetArgumentOrValue<string>("currencyCode"),
                            ObjectIds = ids.ToArray(),
                            IncludeFields = includeFields.ToArray()
                        };

                        var response = await mediator.Send(request);

                        return response.Products.ToDictionary(x => x.Id);
                    });
                    return loader.LoadAsync(context.Source.ProductId);
                })
            });
        }
    }
}
