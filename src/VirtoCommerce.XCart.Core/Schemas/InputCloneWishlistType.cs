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
        Field<StringGraphType>("sharingKey").Description("Sharing key (URL argument)");
        Field<ListGraphType<NonNullGraphType<StringGraphType>>>("addSharedWithIds")
            .Description("Ids to share the list with (id space defined by scope); ids already shared with are ignored; requires scope in the same write; ignored by scopes without targets");
        Field<ListGraphType<NonNullGraphType<StringGraphType>>>("removeSharedWithIds")
            .Description("Ids to stop sharing the list with; ids not shared with are ignored; requires scope in the same write; ignored by scopes without targets");
        Field<StringGraphType>("message")
            .Description("Message saved with the share (one for all targets), max 1024 characters; null leaves it unchanged, an empty or whitespace-only string clears it; requires scope in the same write; ignored by scopes without targets");
        Field<StringGraphType>("description").Description("List description");
    }
}
