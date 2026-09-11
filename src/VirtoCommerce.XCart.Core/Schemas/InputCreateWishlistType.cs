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
            Field<StringGraphType>("sharingKey").Description("Sharing key (URL argument)");
            Field<StringGraphType>("sharedWithId").Description("Id of the principal to share the list with (id space defined by scope)").DeprecationReason("Use addSharedWithIds");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>("addSharedWithIds").Description("Ids of the principals to share the list with (id space defined by scope); ids already shared with are ignored; ignored by scopes without targets");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>("removeSharedWithIds").Description("Ids of the principals to stop sharing the list with; ids not shared with are ignored; ignored by scopes without targets");
            Field<StringGraphType>("message").Description("Message saved with the share (one for all targets); null leaves it unchanged, an empty or whitespace-only string clears it; ignored by scopes without targets");
            Field<StringGraphType>("description").Description("List description");
        }
    }
}
