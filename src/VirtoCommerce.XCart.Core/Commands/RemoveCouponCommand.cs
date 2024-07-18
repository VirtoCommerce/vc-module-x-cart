using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class RemoveCouponCommand : CartCommand
    {
        public string CouponCode { get; set; }
    }
}
