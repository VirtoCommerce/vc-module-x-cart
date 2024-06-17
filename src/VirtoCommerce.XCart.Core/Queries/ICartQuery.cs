using VirtoCommerce.Xapi.Core.Index;

namespace VirtoCommerce.XCart.Core.Queries
{
    public interface ICartQuery : IHasIncludeFields
    {
        string StoreId { get; set; }
        string CartType { get; set; }
        string CartName { get; set; }
        string UserId { get; set; }
        string CurrencyCode { get; set; }
        string CultureName { get; set; }
    }
}
