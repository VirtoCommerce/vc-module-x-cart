namespace VirtoCommerce.XCart.Core.Models
{
    /// <summary>
    /// Used in cart bulk mutations
    /// </summary>
    public class NewBulkCartItem
    {
        public string ProductSku { get; set; }

        public int Quantity { get; set; }
    }
}
