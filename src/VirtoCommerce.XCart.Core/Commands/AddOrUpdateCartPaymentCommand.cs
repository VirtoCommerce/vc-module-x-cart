using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddOrUpdateCartPaymentCommand : CartCommand
    {
        public AddOrUpdateCartPaymentCommand()
        {
        }

        public AddOrUpdateCartPaymentCommand(string storeId, string cartType, string cartName, string userId, string currencyCode, string cultureName, ExpCartPayment payment)
            : base(storeId, cartType, cartName, userId, currencyCode, cultureName)
        {
            Payment = payment;
        }

        public ExpCartPayment Payment { get; set; }
    }
}
