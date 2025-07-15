using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class SaveForLaterItemsCommand : CartCommand
    {
        public string[] LineItemIds { get; set; }
    }
}
