using System;
using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class UpdateCartQuantityCommand : CartCommand, ICloneable
    {
        public IList<UpdateCartQuantityItem> Items { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
