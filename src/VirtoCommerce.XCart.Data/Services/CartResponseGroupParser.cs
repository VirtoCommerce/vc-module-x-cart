using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartResponseGroupParser : ICartResponseGroupParser
    {
        public virtual string GetResponseGroup(IList<string> includeFields)
        {
            var result = CartResponseGroup.Full;

            // Disable recalculation of totals, XAPI will do it on its own
            result &= ~CartResponseGroup.RecalculateTotals;

            if (!includeFields.Any(x => x.Contains("dynamicProperties")))
            {
                // Disable load of dynamic properties if not requested
                result &= ~CartResponseGroup.WithDynamicProperties;
            }

            return result.ToString();
        }
    }
}
