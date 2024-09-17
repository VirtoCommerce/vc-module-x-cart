using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Specifications;

public class PackSizeLimitSpecification
{
    public virtual bool IsSatisfiedBy(CartProduct product, long requestedQuantity)
    {
        var packSize = product.Product.PackSize;
        return packSize == 1 || (requestedQuantity % packSize == 0);
    }
}
