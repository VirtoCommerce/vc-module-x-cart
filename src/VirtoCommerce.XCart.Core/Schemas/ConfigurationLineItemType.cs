using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ConfigurationLineItemType : ExtendableGraphType<ExpConfigurationLineItem>
    {
        public ConfigurationLineItemType(
            IMediator mediator,
            IDataLoaderContextAccessor dataLoader,
            ICurrencyService currencyService)
        {
            Field(x => x.Item.Id, nullable: true).Description("Item id");
            Field(x => x.Item.Quantity, nullable: true).Description("Quantity");

            var productField = new FieldType
            {
                Name = "product",
                Type = GraphTypeExtenstionHelper.GetActualType<ProductType>(),
                Resolver = new FuncFieldResolver<ExpConfigurationLineItem, IDataLoaderResult<ExpProduct>>(context =>
                {
                    var includeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
                    var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>("configurationLineItems_products", async (ids) =>
                    {
                        var userId = context.GetArgumentOrValue<string>("userId") ?? context.Source.UserId;
                        var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? context.Source.CultureName;
                        var storeId = context.Source.StoreId;
                        var currencyCode = context.Source.Currency.Code;

                        var request = new LoadProductsQuery
                        {
                            StoreId = storeId,
                            CurrencyCode = currencyCode,
                            UserId = userId,
                            ObjectIds = ids.ToArray(),
                            IncludeFields = includeFields.ToArray(),
                        };

                        var allCurrencies = await currencyService.GetAllCurrenciesAsync();
                        context.SetCurrencies(allCurrencies, cultureName);
                        context.UserContext.TryAdd("currencyCode", currencyCode);
                        context.UserContext.TryAdd("storeId", storeId);
                        context.UserContext.TryAdd("cultureName", cultureName);

                        var response = await mediator.Send(request);

                        return response.Products.ToDictionary(x => x.Id);
                    });
                    return loader.LoadAsync(context.Source.Item.ProductId);
                })
            };
            AddField(productField);

            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.Source.Currency);

            Field<NonNullGraphType<MoneyType>>("listPrice",
                "List price",
                resolve: context => context.Source.Item.ListPrice.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("extendedPrice",
                "Extended price",
                resolve: context => context.Source.Item.ExtendedPrice.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("salePrice",
                "Sale price",
                resolve: context => context.Source.Item.SalePrice.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("discountAmount",
                "Total discount amount",
                resolve: context => context.Source.Item.DiscountAmount.ToMoney(context.Source.Currency));
        }
    }
}
