using System;

namespace VirtoCommerce.XCart.Core.Models
{
    [Obsolete("Use CartResponseGroup", DiagnosticId = "VC0009", UrlFormat = "https://docs.virtocommerce.org/platform/user-guide/versions/virto3-products-versions/")]
    [Flags]
    public enum CartAggregateResponseGroup
    {
        Default = 0,
        WithPayments = 1,
        WithLineItems = 1 << 1,
        WithShipments = 1 << 2,
        WithDynamicProperties = 1 << 3,
        RecalculateTotals = 1 << 4,
        Full = Default | WithPayments | WithLineItems | WithShipments | WithDynamicProperties | RecalculateTotals
    }
}
