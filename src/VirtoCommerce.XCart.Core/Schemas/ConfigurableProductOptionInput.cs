using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class ConfigurableProductOptionInput : InputObjectGraphType<ConfigurableProductOption>
{
    public ConfigurableProductOptionInput()
    {
        Field<NonNullGraphType<StringGraphType>>("productId");
        Field<NonNullGraphType<IntGraphType>>("quantity");
    }
}
