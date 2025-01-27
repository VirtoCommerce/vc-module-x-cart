using GraphQL.Types;

namespace VirtoCommerce.XPurchase.Schemas
{
    public class InputCreateWishlistType : InputObjectGraphType
    {
        public InputCreateWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId").Description("Store ID");
            Field<NonNullGraphType<StringGraphType>>("userId").Description("Owner ID");
            Field<StringGraphType>("listName").Description("List name");
            Field<StringGraphType>("cultureName").Description("Culture name");
            Field<StringGraphType>("currencyCode").Description("Currency code");
            Field<StringGraphType>("scope").Description("List scope (private or organization)");
            Field<StringGraphType>("description").Description("List description");
        }
    }
}
