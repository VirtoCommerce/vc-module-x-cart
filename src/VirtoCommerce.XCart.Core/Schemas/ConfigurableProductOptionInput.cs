using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurableProductOptionInput : InputObjectGraphType<ConfigurableProductOption>
{
    public ConfigurableProductOptionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("productId").Description("Product ID");
        Field<NonNullGraphType<IntGraphType>>("quantity").Description("Quantity of product");
        Field<BooleanGraphType>("selectedForCheckout").Description("Whether the configuration item is selected for checkout");
    }
}
