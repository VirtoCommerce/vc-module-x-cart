using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartItemsQuantityCommand : CartCommand
    {
        public IList<CartItemQuantity> CartItems { get; set; } = new List<CartItemQuantity>();
    }
}
