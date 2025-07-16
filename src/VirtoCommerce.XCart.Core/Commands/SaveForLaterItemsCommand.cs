using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XPurchase.Commands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class SaveForLaterItemsCommand : CartCommandBase, ICommand<CartAggregateWithList>
    {
        public string CartId { get; set; }
        public string[] LineItemIds { get; set; }
    }
}
