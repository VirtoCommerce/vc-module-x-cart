using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public interface ICartConfigurationCommand
{
    string LineItemId { get; }

    IList<ProductConfigurationSection> ConfigurationSections { get; }
}
