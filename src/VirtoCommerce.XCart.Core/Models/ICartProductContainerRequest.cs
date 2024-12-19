namespace VirtoCommerce.XCart.Core.Models;

public interface ICartProductContainerRequest
{
    string StoreId { get; set; }
    string UserId { get; set; }
    string OrganizationId { get; set; }
    string CurrencyCode { get; set; }
    string CultureName { get; set; }
}
