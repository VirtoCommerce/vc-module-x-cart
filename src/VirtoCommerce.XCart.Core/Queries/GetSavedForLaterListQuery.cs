using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries
{
    public partial class GetSavedForLaterListQuery : IQuery<CartAggregate>, ICartRequest
    {
        public string StoreId { get; set; }
        public string CartType { get; set; }
        public string CartName { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string CurrencyCode { get; set; }
        public string CultureName { get; set; }
    }
}
