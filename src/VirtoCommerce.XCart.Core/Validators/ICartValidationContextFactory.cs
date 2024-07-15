using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Validators
{
    public interface ICartValidationContextFactory
    {
        Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate);
        Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate, IList<CartProduct> products);
    }
}
