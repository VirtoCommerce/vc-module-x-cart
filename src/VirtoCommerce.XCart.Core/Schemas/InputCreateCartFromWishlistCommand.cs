using GraphQL.Types;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputCreateCartFromWishlistCommand : InputObjectGraphType<CreateCartFromWishlistCommand>
{
    public InputCreateCartFromWishlistCommand()
    {
        Field<NonNullGraphType<StringGraphType>>("listId").Description("Wishlist ID");
    }
}
