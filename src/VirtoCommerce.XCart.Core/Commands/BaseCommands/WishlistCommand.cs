using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands.BaseCommands
{
    public abstract class WishlistCommand : CartCommand
    {
        public string ListId { get; set; }

        public WishlistUserContext WishlistUserContext { get; set; }
    }
}
