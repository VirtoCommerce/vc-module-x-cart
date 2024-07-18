using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddWishlistItemsCommand : WishlistCommand
    {
        public IList<NewCartItem> ListItems { get; set; } = new List<NewCartItem>();
    }
}
