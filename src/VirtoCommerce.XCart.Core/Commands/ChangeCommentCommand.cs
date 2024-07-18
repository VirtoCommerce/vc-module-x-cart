using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCommentCommand : CartCommand
    {
        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }
    }
}
