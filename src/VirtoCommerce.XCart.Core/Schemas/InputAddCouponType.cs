using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddCouponType : InputCartBaseType
    {
        public InputAddCouponType()
        {
            Field<NonNullGraphType<StringGraphType>>("couponCode",
                "Coupon code");
        }
    }
}
