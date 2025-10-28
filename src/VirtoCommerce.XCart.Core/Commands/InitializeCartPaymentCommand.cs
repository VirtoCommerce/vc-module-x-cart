using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class InitializeCartPaymentCommand : CartCommand, ICommand<InitializeCartPaymentResult>
{
    // todo: is it necessary here?
    public string PaymentId { get; set; }
}
