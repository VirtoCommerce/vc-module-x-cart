using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddCouponCommand : CartCommand
    {
        public string CouponCode { get; set; }
    }
}
