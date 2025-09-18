using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistAccessType : EnumerationGraphType
    {
        public WishlistAccessType()
        {
            Add(CartSharingAccess.Read, value: CartSharingAccess.Read, description: "Readonly access");
            Add(CartSharingAccess.Write, value: CartSharingAccess.Write, description: "Write access");
        }
    }
}
