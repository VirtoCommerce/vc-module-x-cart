using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputSaveForLaterType : InputObjectGraphType
    {
        public InputSaveForLaterType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId").Description("Store ID");
            Field<NonNullGraphType<StringGraphType>>("userId").Description("Owner ID");
            Field<StringGraphType>("cultureName").Description("Culture name");
            Field<StringGraphType>("currencyCode").Description("Currency code");

            Field<NonNullGraphType<StringGraphType>>("cartId").Description("Source Cart ID");
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("lineItemIds").Description("Line item IDs to save for later");
        }
    }
}
