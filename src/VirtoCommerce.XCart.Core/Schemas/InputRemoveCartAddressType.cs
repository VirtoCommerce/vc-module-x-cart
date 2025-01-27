using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveCartAddressType : InputCartBaseType
    {
        public InputRemoveCartAddressType()
        {
            Field<NonNullGraphType<StringGraphType>>("addressId")
                .Description("Address Id");
        }
    }
}
