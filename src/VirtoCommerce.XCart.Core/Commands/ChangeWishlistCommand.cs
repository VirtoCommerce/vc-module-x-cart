using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeWishlistCommand : ScopedWishlistCommand
    {
        public string ListName { get; set; }

        public string Description { get; set; }
    }
}
