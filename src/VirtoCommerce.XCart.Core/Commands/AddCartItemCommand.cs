using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddCartItemCommand : CartCommand, IHasConfigurationSections
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }

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

        /// <summary>
        /// Configurable product sections
        /// </summary>
        public IList<ProductConfigurationSection> ConfigurationSections { get; set; }
    }
}
