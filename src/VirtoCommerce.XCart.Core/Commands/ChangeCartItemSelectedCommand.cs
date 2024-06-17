using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartItemSelectedCommand : CartCommand
    {
        public string LineItemId { get; set; }

        public bool SelectedForCheckout { get; set; }
    }
}
