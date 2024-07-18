using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddOrUpdateCartPaymentCommand : CartCommand
    {
        public ExpCartPayment Payment { get; set; }
    }
}
