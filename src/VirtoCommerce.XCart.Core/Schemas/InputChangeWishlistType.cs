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
            Field<StringGraphType>("sharedWithId").Description("Id of the principal the list is shared with (id space defined by scope)");
            Field<StringGraphType>("description").Description("List description");
            Field<StringGraphType>("cultureName").Description("Culture name");
        }
    }
}
