using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class UpdateCartDynamicPropertiesCommand : CartCommand
    {
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}
