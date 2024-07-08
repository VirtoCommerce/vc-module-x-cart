using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddCartAddressCommand : CartCommand
    {
        public ExpCartAddress Address { get; set; }
    }
}
