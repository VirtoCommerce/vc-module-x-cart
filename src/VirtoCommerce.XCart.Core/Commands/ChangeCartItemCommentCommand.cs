using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartItemCommentCommand : CartCommand
    {
        public string LineItemId { get; set; }
        public string Comment { get; set; }
    }
}
