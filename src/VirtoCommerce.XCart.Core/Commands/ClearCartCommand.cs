using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ClearCartCommand : CartCommand
    {
        public ClearCartCommand()
            : base()
        {
        }
        public ClearCartCommand(string storeId, string type, string cartName, string userId, string currencyCode, string cultureName)
            : base(storeId, type, cartName, userId, currencyCode, cultureName)
        {
        }
    }
}
