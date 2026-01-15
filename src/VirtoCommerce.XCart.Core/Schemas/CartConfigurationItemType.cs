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
            Field(x => x.SectionId, nullable: false).Description("Configuration item section ID");
            Field(x => x.Type, nullable: false).Description("Configuration item type. Possible values: 'Product', 'Variation', 'Text', 'File'");
            Field(x => x.ProductId, nullable: true).Description("Configuration item product ID");
            Field(x => x.Name, nullable: true).Description("Configuration item name");
            Field(x => x.Sku, nullable: true).Description("Configuration item SKU");
            Field(x => x.ImageUrl, nullable: true).Description("Configuration item image URL");
            Field(x => x.Quantity, nullable: true).Description("Configuration item product quantity");
            Field(x => x.CustomText, nullable: true).Description("Custom text for 'Text' configuration item section");
            Field(x => x.SelectedForCheckout, nullable: false).Description("Whether the configuration item is selected for checkout");

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

            var productField = new FieldType
            {
                Name = "product",
                Type = GraphTypeExtensionHelper.GetActualType<ProductType>(),
                Resolver = new FuncFieldResolver<ConfigurationItem, IDataLoaderResult<ExpProduct>>(context =>
                    dataLoader.LoadCartProduct(context, mediator, currencyService, "cart_configurationItems_products", context.Source.ProductId)),
            };
            AddField(productField);
        }
    }
}
