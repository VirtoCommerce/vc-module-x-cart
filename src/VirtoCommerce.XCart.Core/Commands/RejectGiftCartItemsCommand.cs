using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RejectGiftCartItemsCommand : CartCommand
    {
        public RejectGiftCartItemsCommand()
        {
        }

        public RejectGiftCartItemsCommand(string storeId, string cartType, string cartName, string userId, string currencyCode, string cultureName, IReadOnlyCollection<string> ids)
            : base(storeId, cartType, cartName, userId, currencyCode, cultureName)
        {
            Ids = ids;
        }

        /// <summary>
        /// Ids of gift items to remove
        /// </summary>
        public IReadOnlyCollection<string> Ids { get; set; }
    }
}
