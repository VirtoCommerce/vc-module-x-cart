using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCartItemsCommand : CartCommand
    {
        public string[] LineItemIds { get; set; }
    }
}
