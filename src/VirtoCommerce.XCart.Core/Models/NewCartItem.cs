using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XCart.Core.Models
{
    public class NewCartItem
    {
        public NewCartItem(string productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        public string ProductId { get; private set; }

        public CartProduct CartProduct { get; set; }

        public int Quantity { get; private set; }

        /// <summary>
        /// Manual price
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Dynamic properties
        /// </summary>
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }

        public bool IsWishlist { get; set; }

        public bool? IsSelectedForCheckout { get; set; }

        public bool IgnoreValidationErrors { get; set; }
    }
}
