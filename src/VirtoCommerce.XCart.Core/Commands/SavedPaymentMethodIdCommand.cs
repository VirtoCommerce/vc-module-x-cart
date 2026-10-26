using MediatR;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.XCart.Core.Commands;

public abstract class SavedPaymentMethodIdCommand : IRequest<StoredPaymentMethod>, ISavedPaymentMethodRequest
{
    public string Id { get; set; }
    public string UserId { get; set; }
}
