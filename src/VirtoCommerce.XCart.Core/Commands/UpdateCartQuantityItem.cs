namespace VirtoCommerce.XCart.Core.Commands
{
    public class UpdateCartQuantityItem
    {
        public string ProductId { get; set; }

        public int Quantity { get; set; }

        public string CurrencyCode { get; set; }
    }
}
