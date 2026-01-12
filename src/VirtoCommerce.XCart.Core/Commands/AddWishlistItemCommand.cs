using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddWishlistItemCommand : WishlistCommand
    {
        public string ProductId { get; set; }
        public int? Quantity { get; set; }

        public IList<ProductConfigurationSection> ConfigurationSections { get; set; }

        public AddWishlistItemCommand(string listId, string productId)
        {
            ListId = listId;
            ProductId = productId;
        }

        public AddWishlistItemCommand()
        {
        }
    }
}
