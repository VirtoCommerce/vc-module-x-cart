using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public abstract class InputCartBaseType : InputObjectGraphType
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
