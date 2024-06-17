using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartItemsSelectedCommand : CartCommand
    {
        public IList<string> LineItemIds { get; set; } = new List<string>();

        public bool SelectedForCheckout { get; set; }
    }
}
