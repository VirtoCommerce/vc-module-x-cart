using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Specifications
{
    public class ProductMinQuantityAvailableSpecification
    {
        public virtual bool IsSatisfiedBy(CartProduct product, int? minQuantity)
        {
            var result = true;

            if (minQuantity.HasValue && minQuantity.Value > 0 && product.Product.TrackInventory.GetValueOrDefault(false) && product.Inventory != null)
            {
                result = product.Inventory.AllowPreorder || product.Inventory.AllowBackorder || product.AvailableQuantity >= minQuantity.Value;
            }

            return result;
        }
    }
}
