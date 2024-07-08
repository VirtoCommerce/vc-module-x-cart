using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RejectGiftCartItemsCommand : CartCommand
    {
        /// <summary>
        /// Ids of gift items to remove
        /// </summary>
        public IReadOnlyCollection<string> Ids { get; set; }
    }
}
