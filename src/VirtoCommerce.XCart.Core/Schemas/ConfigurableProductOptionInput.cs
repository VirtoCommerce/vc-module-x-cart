using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurableProductOptionInput : InputObjectGraphType<ConfigurableProductOption>
{
    public ConfigurableProductOptionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("productId")
            .Description("Catalog item ID");
        Field<NonNullGraphType<IntGraphType>>("quantity")
            .Description("Quantity of product");
    }
}
