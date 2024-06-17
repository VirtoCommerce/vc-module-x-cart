using System.Threading.Tasks;

namespace VirtoCommerce.XCart.Core.Validators
{
    public interface ICartValidationContextFactory
    {
        Task<CartValidationContext> CreateValidationContextAsync(CartAggregate cartAggregate);
    }
}
