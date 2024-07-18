using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveWishlistItemsCommand : WishlistCommand
    {
        public IList<string> LineItemIds { get; set; } = new List<string>();
    }
}
