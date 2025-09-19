using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class CartPickupAvailabilityType : EnumerationGraphType
{
    public CartPickupAvailabilityType()
    {
        Add(CartPickupAvailability.Today, value: CartPickupAvailability.Today, description: "Available today (within hours)");
        Add(CartPickupAvailability.Transfer, value: CartPickupAvailability.Transfer, description: "Available via transfer (within days)");
        Add(CartPickupAvailability.GlobalTransfer, value: CartPickupAvailability.GlobalTransfer, description: "Available via global transfer (within weeks)");
    }
}
