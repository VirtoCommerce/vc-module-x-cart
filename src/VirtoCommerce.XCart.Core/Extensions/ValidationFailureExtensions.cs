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
            ArgumentNullException.ThrowIfNull(errors);
            ArgumentNullException.ThrowIfNull(entity);

            return errors.OfType<CartValidationError>().Where(x => x.ObjectType.EqualsIgnoreCase(entity.GetType().Name) && x.ObjectId == entity.Id);
        }
    }
}
