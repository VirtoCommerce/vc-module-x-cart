using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartWithListType : ExtendableGraphType<CartAggregateWithList>
    {
        public CartWithListType()
        {
            ExtendableField<CartType>("cart", "Shopping cart", resolve: context => context.Source.Cart);
            ExtendableField<CartType>("list", "Saved list", resolve: context => context.Source.List);
        }
    }
}
