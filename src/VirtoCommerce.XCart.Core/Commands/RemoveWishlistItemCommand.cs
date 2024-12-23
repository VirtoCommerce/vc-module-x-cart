using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveWishlistItemCommand : WishlistCommand
    {
        public string LineItemId { get; set; }

        public string ProductId { get; set; }

        public RemoveWishlistItemCommand()
        {
        }

        public RemoveWishlistItemCommand(string listId, string lineItemId, string productId)
        {
            ListId = listId;
            LineItemId = lineItemId;
            ProductId = productId;
        }
    }
}
