using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class ValidationErrorType : ExtendableGraphType<CartValidationError>
    {
        public ValidationErrorType()
        {
            Field(x => x.ErrorCode, nullable: true).Description("Error code");
            Field(x => x.ErrorMessage, nullable: true).Description("Error message");
            Field(x => x.ObjectId, nullable: true).Description("Object id");
            Field(x => x.ObjectType, nullable: true).Description("Object type");
            Field<ListGraphType<ErrorParameterType>>("errorParameters").Resolve(x => x.Source.ErrorParameters);
        }
    }
}
