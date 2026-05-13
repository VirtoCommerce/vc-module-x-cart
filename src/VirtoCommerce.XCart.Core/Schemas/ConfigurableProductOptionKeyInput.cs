using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

/// <summary>
/// Identifying subset of <see cref="ConfigurableProductOption"/> — only the fields required
/// to locate an existing configuration item. <c>Quantity</c> and <c>SelectedForCheckout</c>
/// are intentionally omitted because mutations that use this type do not modify them.
/// </summary>
public class ConfigurableProductOptionKeyInput : InputObjectGraphType<ConfigurableProductOption>
{
    public ConfigurableProductOptionKeyInput()
    {
        Field<NonNullGraphType<StringGraphType>>("productId").Description("Product ID");
    }
}
