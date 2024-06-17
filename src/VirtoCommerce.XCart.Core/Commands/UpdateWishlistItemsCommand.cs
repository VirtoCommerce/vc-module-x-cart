using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class UpdateWishlistItemsCommand : WishlistCommand
    {
        public List<WishListItem> Items { get; set; } = new List<WishListItem>();
    }
}
