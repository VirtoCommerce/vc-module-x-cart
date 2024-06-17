using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputMergeCartType : InputCartBaseType
    {
        public InputMergeCartType()
        {
            Field<NonNullGraphType<StringGraphType>>("secondCartId", "Second cart Id");
            Field<BooleanGraphType>("deleteAfterMerge", "Delete second cart after merge");
        }
    }
}
