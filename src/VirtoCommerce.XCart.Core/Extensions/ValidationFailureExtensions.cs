using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Extensions
{
    public static class ValidationFailureExtensions
    {
        public static IEnumerable<CartValidationError> GetEntityCartErrors(this IEnumerable<ValidationFailure> errors, IEntity entity)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            return errors.OfType<CartValidationError>().Where(x => x.ObjectType.EqualsInvariant(entity.GetType().Name) && x.ObjectId == entity.Id);
        }
    }
}
