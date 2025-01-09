using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class BulkCartType : ExtendableGraphType<BulkCartResult>
    {
        public BulkCartType()
        {
            ExtendableField<CartType>("cart",
                "Cart",
                resolve: context => context.Source.Cart);

            Field<ListGraphType<ValidationErrorType>>("errors")
                .Description("A set of errors in case the SKUs are invalid")
                .Resolve(context => context.Source.Errors);
        }
    }
}
