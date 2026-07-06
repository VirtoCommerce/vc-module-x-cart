using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class AddConfigurationItemCommand : CartCommand, ICartConfigurationCommand
{
    public string LineItemId { get; set; }

    public ProductConfigurationSection ConfigurationSection { get; set; }

    [SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null",
        Justification = "null is intentional, consistent with the nullable ICartConfigurationCommand contract; the sole consumer guards IsNullOrEmpty, so null is safe and avoids a throwaway single-element list.")]
    public IList<ProductConfigurationSection> ConfigurationSections =>
        ConfigurationSection is null ? null : [ConfigurationSection];
}
