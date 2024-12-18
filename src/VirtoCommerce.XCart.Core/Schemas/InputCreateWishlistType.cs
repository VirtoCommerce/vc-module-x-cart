using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XPurchase.Schemas
{
    public class InputCreateWishlistType : ExtendableInputGraphType
    {
        public InputCreateWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", description: "Store ID");
            Field<NonNullGraphType<StringGraphType>>("userId", description: "Owner ID");
            Field<StringGraphType>("listName", description: "List name");
            Field<StringGraphType>("cultureName", description: "Culture name");
            Field<StringGraphType>("currencyCode", description: "Currency code");
            Field<StringGraphType>("scope", description: "List scope (private or organization)");
            Field<StringGraphType>("description", description: "List description");
        }
    }
}
