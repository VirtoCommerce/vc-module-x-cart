using System.Collections.Generic;
using MediatR;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Core.Commands;

public class AddSavedPaymentMethodCommand : IRequest<StoredPaymentMethod>, ISavedPaymentMethodRequest
{
    public string StoreId { get; set; }
    public string PaymentMethodCode { get; set; }
    public IList<KeyValue> Parameters { get; set; }
    public bool? SetDefault { get; set; }
    public string UserId { get; set; }
}
