using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XPurchase.Commands;

namespace VirtoCommerce.XCart.Core.Commands.BaseCommands
{
    public abstract class CartCommand : CartCommandBase, ICommand<CartAggregate>
    {
        public string CartId { get; set; }
    }
}
