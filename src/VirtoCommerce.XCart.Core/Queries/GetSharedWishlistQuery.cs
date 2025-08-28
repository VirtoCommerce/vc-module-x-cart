using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Queries
{
    public partial class GetSharedWishlistQuery : IQuery<CartAggregate>
    {
        public string SharingKey { get; set; }

        public IList<string> IncludeFields { get; set; }
    }
}
