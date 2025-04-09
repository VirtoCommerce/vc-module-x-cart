using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core
{
    public class ExpPricesSum
    {
        public decimal ListPriceSum { get; set; }

        public decimal SalePriceSum { get; set; }

        public decimal DiscountAmountSum { get; set; }

        public Currency Currency { get; set; }
    }
}
