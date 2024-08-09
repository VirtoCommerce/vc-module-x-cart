using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Models
{
    public class CartValidationError : ValidationFailure
    {
        public CartValidationError(IEntity entity, string error, string errorCode = null)
            : base(entity.ToString(), error)
        {
            ObjectType = entity.GetType().Name;
            ObjectId = entity.Id;
            ErrorMessage = error;
            ErrorCode = errorCode;
        }

        public CartValidationError(string type, string id, string error, string errorCode = null)
            : base(type, error)
        {
            ObjectType = type;
            ObjectId = id;
            ErrorMessage = error;
            ErrorCode = errorCode;
        }

        public string ObjectType { get; set; }
        public string ObjectId { get; set; }
        public List<ErrorParameter> ErrorParameters =>
            FormattedMessagePlaceholderValues
                ?.Select(kvp =>
                {
                    if (kvp.Value is List<string> values)
                    {
                        return new ErrorParameter { Key = kvp.Key, Value = string.Join(',', values) };
                    }
                    else
                    {
                        return new ErrorParameter { Key = kvp.Key, Value = kvp.Value.ToString() };
                    }
                })
                .ToList();
    }
}
