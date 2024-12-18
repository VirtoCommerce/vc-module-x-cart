using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveCartType : ExtendableInputGraphType
    {
        public InputRemoveCartType()
        {
            Field<NonNullGraphType<StringGraphType>>("cartId", "Cart Id");
            Field<NonNullGraphType<StringGraphType>>("userId", "User Id");
        }
    }
}
