using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class UpdateConfigurationItemCommand : CartCommand
{
    public string LineItemId { get; set; }

    public ProductConfigurationSection ConfigurationSection { get; set; }
}
