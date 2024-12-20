using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ChangeCartCurrencyCommand : CartCommand
    {
        public string NewCurrencyCode { get; set; }
    }
}
