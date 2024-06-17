using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models
{
    public class BulkCartAggregateResult
    {
        public IList<CartAggregate> CartAggregates { get; set; } = new List<CartAggregate>();
    }
}
