using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class MergeCartCommand : CartCommand
    {
        public string SecondCartId { get; set; }

        public bool DeleteAfterMerge { get; set; } = true;
    }
}
