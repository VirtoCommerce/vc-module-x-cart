using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XPurchase.Commands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddCartItemsBulkCommand : CartCommandBase, ICommand<BulkCartResult>
    {
        public string CartId { get; set; }
        public IList<NewBulkCartItem> CartItems { get; set; } = new List<NewBulkCartItem>();
    }
}
