using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class MoveSavedForLaterItemsCommandBase : CartCommand, ICommand<CartAggregateWithList>
    {
        public string[] LineItemIds { get; set; }

        public CartAggregate Cart { get; set; }
    }
}
