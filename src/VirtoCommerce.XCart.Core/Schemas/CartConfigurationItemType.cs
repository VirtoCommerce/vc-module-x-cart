using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartConfigurationItemType : ExtendableGraphType<ConfigurationItem>
    {
        public CartConfigurationItemType(
            IMediator mediator,
            IDataLoaderContextAccessor dataLoader,
            ICurrencyService currencyService)
        {
            Field(x => x.Id, nullable: false).Description("Configuration item ID");
            Field(x => x.Name, nullable: true).Description("Configuration item name");
            Field(x => x.SectionId, nullable: false).Description("Configuration item section ID");
            Field(x => x.ProductId, nullable: true).Description("Configuration item product ID");
            Field(x => x.Quantity, nullable: true).Description("Configuration item product quantity");
            Field(x => x.CustomText, nullable: true).Description("Custom text for 'Text' configuration item section");
            Field(x => x.Type, nullable: false).Description("Configuration item type. Possible values: 'Product', 'Variation', 'Text', 'File'");

            Field<NonNullGraphType<MoneyType>>("listPrice")
                .Description("List price")
                .Resolve(context => context.Source.ListPrice.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("salePrice")
                .Description("Sale price")
                .Resolve(context => context.Source.SalePrice.ToMoney(context.GetCart().Currency));

            Field<NonNullGraphType<MoneyType>>("extendedPrice")
                .Description("Extended price")
                .Resolve(context => context.Source.ExtendedPrice.ToMoney(context.GetCart().Currency));

            ExtendableField<ListGraphType<CartConfigurationItemFileType>>(nameof(ConfigurationItem.Files),
                resolve: context => context.Source.Files,
                description: "List of files for 'File' configuration item section");

            // Add variation field for loading full variation data
            var variationField = new FieldType
            {
                Name = "variation",
                Type = GraphTypeExtensionHelper.GetActualType<VariationType>(),
                Resolver = new FuncFieldResolver<ConfigurationItem, IDataLoaderResult<ExpProduct>>(context =>
                {
                    // Only load variation if type is "Variation" and productId is present
                    if (context.Source.Type != "Variation" || string.IsNullOrEmpty(context.Source.ProductId))
                    {
                        return null;
                    }

                    var includeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
                    var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>("configuration_item_variations", async (ids) =>
                    {
                        var cartAggregate = context.GetValueForSource<CartAggregate>();
                        var cart = cartAggregate.Cart;
                        var userId = context.GetArgumentOrValue<string>("userId") ?? cart.CustomerId;

                        var request = new LoadProductsQuery
                        {
                            StoreId = cart.StoreId,
                            CurrencyCode = cart.Currency,
                            ObjectIds = ids.ToArray(),
                            IncludeFields = includeFields.ToArray(),
                            UserId = userId,
                            OrganizationId = context.GetCurrentOrganizationId(),
                        };

                        var allCurrencies = await currencyService.GetAllCurrenciesAsync();
                        var cultureName = context.GetArgumentOrValue<string>("cultureName") ?? cart.LanguageCode;
                        context.SetCurrencies(allCurrencies, cultureName);
                        context.UserContext.TryAdd("currencyCode", cart.Currency);
                        context.UserContext.TryAdd("storeId", cart.StoreId);
                        context.UserContext.TryAdd("store", cartAggregate.Store);
                        context.UserContext.TryAdd("cultureName", cultureName);

                        var response = await mediator.Send(request);

                        return response.Products.ToDictionary(x => x.Id);
                    });
                    return loader.LoadAsync(context.Source.ProductId);
                })
            };
            AddField(variationField);
        }
    }
}
