using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    public interface ICartAvailMethodsService
    {
        Task<IEnumerable<PaymentMethod>> GetAvailablePaymentMethodsAsync(CartAggregate cartAggregate);
        Task<IEnumerable<ShippingRate>> GetAvailableShippingRatesAsync(CartAggregate cartAggregate);
        Task<IEnumerable<GiftItem>> GetAvailableGiftsAsync(CartAggregate cartAggregate);
    }
}
