using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputMergeCartType : InputCartBaseType
    {
        public InputMergeCartType()
        {
            Field<NonNullGraphType<StringGraphType>>("secondCartId").Description("Second cart Id");
            Field<BooleanGraphType>("deleteAfterMerge").Description("Delete second cart after merge");
        }
    }
}
