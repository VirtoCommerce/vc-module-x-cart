using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.XCart.Core.Models
{
    public class ExpConfigurationLineItem
    {
        public string StoreId { get; set; }
        public string UserId { get; set; }
        public string CultureName { get; set; }

        public LineItem Item { get; set; }
        public Currency Currency { get; set; }

        public string Id { get; set; }
        public string Text { get; set; }
        public int Quantity { get; set; }
    }
}
