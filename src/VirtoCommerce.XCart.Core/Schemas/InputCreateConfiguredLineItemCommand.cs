using GraphQL.Types;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputCreateConfiguredLineItemCommand : InputObjectGraphType<CreateConfiguredLineItemCommand>
{
    public InputCreateConfiguredLineItemCommand()
    {
        Field<StringGraphType>("storeId");
        Field<StringGraphType>("currencyCode");
        Field<StringGraphType>("cultureName");
        Field<NonNullGraphType<StringGraphType>>("configurableProductId");
        Field<ListGraphType<ConfigurationSectionInput>>("configurationSections");
    }
}
