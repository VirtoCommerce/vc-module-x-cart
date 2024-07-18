using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCartAddressCommand : CartCommand
    {
        public string AddressId { get; set; }
    }
}
