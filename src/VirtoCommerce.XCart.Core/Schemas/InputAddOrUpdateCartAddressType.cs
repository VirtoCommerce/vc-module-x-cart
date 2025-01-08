using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddOrUpdateCartAddressType : InputCartBaseType
    {
        public InputAddOrUpdateCartAddressType()
        {
            Field<NonNullGraphType<InputAddressType>>("address")
                .Description("Address");
        }
    }
}
