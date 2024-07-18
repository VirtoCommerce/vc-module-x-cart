using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeAllCartItemsSelectedCommand : CartCommand
    {
        public bool SelectedForCheckout { get; set; }
    }
}
