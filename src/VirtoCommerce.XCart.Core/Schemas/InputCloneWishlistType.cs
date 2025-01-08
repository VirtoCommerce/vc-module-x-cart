using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputCloneWishlistType : InputObjectGraphType
{
    public InputCloneWishlistType()
    {
        Field<NonNullGraphType<StringGraphType>>("storeId").Description("Store ID");
        Field<NonNullGraphType<StringGraphType>>("userId").Description("Owner ID");
        Field<NonNullGraphType<StringGraphType>>("listId").Description("Source List ID");
        Field<StringGraphType>("listName").Description("List name");
        Field<StringGraphType>("cultureName").Description("Culture name");
        Field<StringGraphType>("currencyCode").Description("Currency code");
        Field<StringGraphType>("scope").Description("List scope (private or organization)");
        Field<StringGraphType>("description").Description("List description");
    }
}
