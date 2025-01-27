using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCartCommand : ICommand<bool>
    {
        public string CartId { get; set; }
        public string UserId { get; set; }
    }
}
