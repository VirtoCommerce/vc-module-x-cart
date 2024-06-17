using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveWishlistCommand : WishlistCommand
    {
        public RemoveWishlistCommand(string listId)
        {
            ListId = listId;
        }
    }
}
