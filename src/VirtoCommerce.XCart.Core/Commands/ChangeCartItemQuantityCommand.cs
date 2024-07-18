using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartItemQuantityCommand : CartCommand
    {
        public string LineItemId { get; set; }
        public int Quantity { get; set; }
    }
}
