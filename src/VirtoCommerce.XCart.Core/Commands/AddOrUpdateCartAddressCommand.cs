using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddOrUpdateCartAddressCommand : CartCommand
    {
        public ExpCartAddress Address { get; set; }
    }
}
