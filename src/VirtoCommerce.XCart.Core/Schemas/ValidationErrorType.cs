using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ValidationErrorType : ObjectGraphType<CartValidationError>
    {
        public ValidationErrorType()
        {
            Field(x => x.ErrorCode, nullable: true).Description("Error code");
            Field(x => x.ErrorMessage, nullable: true).Description("Error message");
            Field(x => x.ObjectId, nullable: true).Description("Object id");
            Field(x => x.ObjectType, nullable: true).Description("Object type");
            Field<ListGraphType<ErrorParameterType>>(name: "errorParameters", resolve: x => x.Source.ErrorParameters);
        }
    }
}
