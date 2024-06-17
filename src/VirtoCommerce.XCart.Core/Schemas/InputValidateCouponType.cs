using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputValidateCouponType : InputCartBaseType
    {
        public InputValidateCouponType()
        {
            Field<NonNullGraphType<StringGraphType>>("coupon",
                "Coupon");
        }
    }
}
