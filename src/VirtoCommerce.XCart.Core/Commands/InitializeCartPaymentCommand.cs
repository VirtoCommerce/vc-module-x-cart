using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class InitializeCartPaymentCommand : ICommand<InitializeCartPaymentResult>
{
    public string CartId { get; set; }
    public string PaymentId { get; set; }
}
