using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core
{
    public class ExpConfigurationLineItem
    {
        public string StoreId { get; set; }
        public string UserId { get; set; }
        public string CultureName { get; set; }

        public LineItem Item { get; set; }
        public Currency Currency { get; set; }
    }
}
