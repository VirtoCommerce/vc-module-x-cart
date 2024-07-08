using MediatR;
using VirtoCommerce.XPurchase.Commands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class ValidateCouponCommand : CartCommandBase, IRequest<bool>
    {
        public string Coupon { get; set; }
    }
}
