using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeWishlistType : InputObjectGraphType
    {
        public InputChangeWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("List ID");
            Field<StringGraphType>("listName").Description("New List name");
            Field<StringGraphType>("scope").Description("List scope (private or organization)");
            Field<StringGraphType>("sharingKey").Description("Sharing key (URL argument)");
            Field<StringGraphType>("sharedWithId").Description("The single recipient the list is shared with (id space defined by scope): replaces the one it currently has, and is refused when the list has several; requires scope in the same write; ignored by scopes without targets").DeprecationReason("Use addSharedWithIds");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>("addSharedWithIds").Description("Ids to share the list with (id space defined by scope); ids already shared with are ignored; requires scope in the same write; ignored by scopes without targets");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>("removeSharedWithIds").Description("Ids to stop sharing the list with; ids not shared with are ignored; requires scope in the same write; ignored by scopes without targets");
            Field<StringGraphType>("message").Description("Message saved with the share (one for all targets), max 1024 characters; null leaves it unchanged, an empty or whitespace-only string clears it; requires scope in the same write; ignored by scopes without targets");
            Field<StringGraphType>("description").Description("List description");
            Field<StringGraphType>("cultureName").Description("Culture name");
        }
    }
}
