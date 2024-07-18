using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddCartItemsCommand : CartCommand
    {
        public NewCartItem[] CartItems { get; set; }
    }
}
