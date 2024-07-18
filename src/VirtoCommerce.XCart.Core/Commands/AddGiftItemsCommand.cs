using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddGiftItemsCommand : CartCommand
    {
        /// <summary>
        /// Ids of rewards to add
        /// </summary>
        public IReadOnlyCollection<string> Ids { get; set; }
    }
}
