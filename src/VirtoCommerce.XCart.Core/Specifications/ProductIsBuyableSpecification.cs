using System;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Specifications
{
    /// <summary>
    /// Represents a product is buyable specification.
    /// </summary>
    public class ProductIsBuyableSpecification
    {
        /// <summary>
        /// Evaluates a product is buyable specification.
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual bool IsSatisfiedBy(CartProduct product)
        {
            ArgumentNullException.ThrowIfNull(product);

            return product.Product.IsActive.GetValueOrDefault(false) &&
                product.Product.IsBuyable.GetValueOrDefault(false) &&
                product.Price != null &&
                CheckPricePolicy(product);
        }

        /// <summary>
        /// Represents a price policy for a product. By default, product price should be greater than zero.
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        protected virtual bool CheckPricePolicy(CartProduct product)
        {
            return product.Price.ActualPrice != 0;
        }
    }
}
