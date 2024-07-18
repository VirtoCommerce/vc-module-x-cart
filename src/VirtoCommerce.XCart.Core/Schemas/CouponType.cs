using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class CouponType : ObjectGraphType<CartCoupon>
    {
        public CouponType()
        {
            Field(x => x.Code, nullable: true).Description("Coupon code");
            Field(x => x.IsAppliedSuccessfully, nullable: false).Description("Is coupon was applied successfully");
        }
    }
}
