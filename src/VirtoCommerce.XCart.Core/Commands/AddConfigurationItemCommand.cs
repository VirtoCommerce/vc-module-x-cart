using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class AddConfigurationItemCommand : CartCommand, ICartConfigurationCommand
{
    public string LineItemId { get; set; }

    public ProductConfigurationSection ConfigurationSection { get; set; }

    public IList<ProductConfigurationSection> ConfigurationSections =>
        ConfigurationSection is null ? null : [ConfigurationSection];
}
