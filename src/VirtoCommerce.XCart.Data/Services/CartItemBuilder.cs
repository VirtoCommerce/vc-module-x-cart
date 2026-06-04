using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services
{
    public class CartItemBuilder : ICartItemBuilder
    {
        public virtual LineItem Create(CartProduct cartProduct)
        {
            return AbstractTypeFactory<LineItem>.TryCreateInstance();
        }

        public virtual ConfigurationItem Create(string sectionId, string type)
        {
            var item = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();
            item.SectionId = sectionId;
            item.Type = type;

            return item;
        }
    }
}
