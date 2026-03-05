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
            Field<DecimalGraphType>("price").Description("Price");
            Field<StringGraphType>("comment").Description("Comment");

            Field<ListGraphType<InputDynamicPropertyValueType>>("dynamicProperties");

            Field<ListGraphType<ConfigurationSectionInput>>("configurationSections")
                .Description("Configurable product sections");

            Field<DateTimeGraphType>("createdDate")
                .Description("Line item created date override");
        }
    }
}
