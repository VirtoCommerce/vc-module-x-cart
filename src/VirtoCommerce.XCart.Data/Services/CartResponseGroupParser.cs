using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartResponseGroupParser : ICartResponseGroupParser
    {
        public virtual string GetResponseGroup(IList<string> includeFields)
        {
            return Core.ModuleConstants.XCartResponseGroup;
        }
    }
}
