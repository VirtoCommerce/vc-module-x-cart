using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class ChangeCartConfiguredItemCommand : CartCommand
{
    public string LineItemId { get; set; }

    public int? Quantity { get; set; }

    public IList<ProductConfigurationSection> ConfigurationSections { get; set; } = new List<ProductConfigurationSection>();
}
