using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Queries
{
    public partial class GetWishlistQuery : IQuery<CartAggregate>
    {
        public string ListId { get; set; }

        public string CultureName { get; set; }

        public IList<string> IncludeFields { get; set; }
    }
}
