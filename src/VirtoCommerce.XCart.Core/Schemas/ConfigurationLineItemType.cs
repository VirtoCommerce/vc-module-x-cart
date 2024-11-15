using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ConfigurationLineItemType : ExtendableGraphType<ExpConfigurationLineItem>
    {
        public ConfigurationLineItemType()
        {
            Field(x => x.Item.Id, nullable: true).Description("Item id");
            Field(x => x.Item.Quantity, nullable: true).Description("Quantity");

            ExtendableField<ProductType>("product",
                "Product",
                resolve: context => context.Source.Product);

            Field<NonNullGraphType<CurrencyType>>("currency",
                "Currency",
                resolve: context => context.Source.Currency);

            Field<NonNullGraphType<MoneyType>>("listPrice",
                "List price",
                resolve: context => context.Source.Item.ListPrice.ToMoney(context.Source.Currency));

            Field<NonNullGraphType<MoneyType>>("extendedPrice",
                "List price",
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
