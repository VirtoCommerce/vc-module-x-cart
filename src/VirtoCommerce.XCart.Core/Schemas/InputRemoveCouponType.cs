using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveCouponType : InputCartBaseType
    {
        public InputRemoveCouponType()
        {
            Field<StringGraphType>("couponCode")
                .Description("Coupon code");
        }
    }
}
