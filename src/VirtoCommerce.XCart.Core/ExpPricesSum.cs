using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core
{
    public class ExpPricesSum
    {
        public decimal Total { get; set; }

        public decimal DiscountTotal { get; set; }

        public Currency Currency { get; set; }
    }
}
