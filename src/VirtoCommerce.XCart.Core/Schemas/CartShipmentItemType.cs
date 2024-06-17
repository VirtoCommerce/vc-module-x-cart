using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CartShipmentItemType : ExtendableGraphType<ShipmentItem>
    {
        public CartShipmentItemType()
        {
            Field(x => x.Quantity, nullable: false).Description("Quantity");
            ExtendableField<LineItemType>("lineItem", resolve: context => context.Source.LineItem);
        }
    }
}
