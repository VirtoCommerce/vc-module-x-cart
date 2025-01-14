using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Queries
{
    public class ValidateCouponQuery : IQuery<bool>, ICartQuery
    {
        public IList<string> IncludeFields { get; set; } = new List<string>();
        public string StoreId { get; set; }
        public string CartType { get; set; }
        public string CartName { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string CurrencyCode { get; set; }
        public string CultureName { get; set; }
        public string Coupon { get; set; }
        public string CartId { get; set; }
    }
}
