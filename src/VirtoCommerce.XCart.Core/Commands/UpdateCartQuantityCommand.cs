using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class UpdateCartQuantityCommand : CartCommand
    {
        public IList<UpdateCartQuantityItem> Items { get; set; }
    }
}
