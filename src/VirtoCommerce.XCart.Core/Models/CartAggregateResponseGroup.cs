using System;

namespace VirtoCommerce.XCart.Core.Models
{
    [Flags]
    public enum CartAggregateResponseGroup
    {
        None = 0,
        WithPayments = 1,
        WithLineItems = 1 << 1,
        WithShipments = 1 << 2,
        Validate = 1 << 3,
        WithDynamicProperties = 1 << 4,
        Full = WithPayments | WithLineItems | WithShipments | Validate | WithDynamicProperties
    }
}
