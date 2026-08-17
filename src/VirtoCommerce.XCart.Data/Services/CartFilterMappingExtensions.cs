using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Xapi.Core.Index;

namespace VirtoCommerce.XCart.Data.Services;

public static class CartFilterMappingExtensions
{
    public static void MapTo(this IList<IFilter> filters, ShoppingCartSearchCriteria criteria)
    {
        if (filters == null || criteria == null)
        {
            return;
        }

        foreach (var term in filters.OfType<TermFilter>())
        {
            term.MapTo(criteria);
        }
    }
}
