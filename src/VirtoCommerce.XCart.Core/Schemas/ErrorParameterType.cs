using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ErrorParameterType : ObjectGraphType<ErrorParameter>
    {
        public ErrorParameterType()
        {
            Field(x => x.Key).Description("key");
            Field(x => x.Value).Description("Value");
        }
    }
}
