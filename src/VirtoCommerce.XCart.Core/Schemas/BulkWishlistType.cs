using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class BulkWishlistType : ExtendableGraphType<BulkCartAggregateResult>
    {
        public BulkWishlistType()
        {
            ExtendableField<ListGraphType<WishlistType>>("wishlists", "Wishlists", resolve: context => context.Source.CartAggregates);
        }
    }
}
