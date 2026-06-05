using System;
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

        public virtual ConfigurationItem Create(ProductConfigurationSection configurationSection, CartProduct cartProduct = null)
        {
            ArgumentNullException.ThrowIfNull(configurationSection);

            var configurationItem = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();
            configurationItem.SectionId = configurationSection.SectionId;
            configurationItem.SectionName = configurationSection.SectionName;
            configurationItem.Type = configurationSection.Type;

            return configurationItem;
        }
    }
}
