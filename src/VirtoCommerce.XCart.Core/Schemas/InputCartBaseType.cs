using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public abstract class InputCartBaseType : ExtendableInputGraphType
    {
        protected InputCartBaseType()
        {
            Field<StringGraphType>("cartId");
            Field<NonNullGraphType<StringGraphType>>("storeId");
            Field<StringGraphType>("cartName");
            Field<NonNullGraphType<StringGraphType>>("userId");
            Field<StringGraphType>("currencyCode");
            Field<StringGraphType>("cultureName");
            Field<StringGraphType>("cartType");
        }
    }
}
