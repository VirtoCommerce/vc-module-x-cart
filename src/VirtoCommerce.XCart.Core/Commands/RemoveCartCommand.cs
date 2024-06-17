using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCartCommand : ICommand<bool>
    {
        public RemoveCartCommand()
        {
        }

        public RemoveCartCommand(string cartId)
        {
            CartId = cartId;
        }

        public string CartId { get; set; }
    }
}
