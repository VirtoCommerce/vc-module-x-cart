using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartResponseGroupParser
    {
        string GetResponseGroup(IList<string> includeFields);
    }
}
