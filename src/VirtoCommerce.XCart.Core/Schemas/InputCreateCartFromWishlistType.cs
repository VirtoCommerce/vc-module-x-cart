using GraphQL.Types;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputCreateCartFromWishlistType : InputObjectGraphType<CreateCartFromWishlistCommand>
{
    public InputCreateCartFromWishlistType()
    {
        Field<NonNullGraphType<StringGraphType>>("listId").Description("Wishlist ID");
    }
}
