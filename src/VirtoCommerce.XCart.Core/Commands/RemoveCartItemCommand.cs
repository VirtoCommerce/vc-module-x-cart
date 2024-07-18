using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCartItemCommand : CartCommand
    {
        public string LineItemId { get; set; }
    }
}
