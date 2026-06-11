using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputNewCartItemType : InputObjectGraphType<NewCartItem>
    {
        public InputNewCartItemType()
        {
            Field(x => x.ProductId, nullable: false).Description("Product Id");
            Field(x => x.Quantity, nullable: true).Description("Product quantity");
            Field<StringGraphType>("itemCurrencyCode").Description("Add the product in a different currency");

            Field<ListGraphType<InputDynamicPropertyValueType>>("dynamicProperties");
        }
    }
}
