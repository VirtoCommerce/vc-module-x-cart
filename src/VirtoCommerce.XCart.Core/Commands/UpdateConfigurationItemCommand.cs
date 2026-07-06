using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands;

public class UpdateConfigurationItemCommand : CartCommand, ICartConfigurationCommand
{
    public string LineItemId { get; set; }

    public ProductConfigurationSection ConfigurationSection { get; set; }

    [SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null",
        Justification = "null is intentional and consistent with the nullable ICartConfigurationCommand contract (the plural commands return null too); the sole consumer CartConfigurationService.UpdateSectionsFromCatalogAsync guards IsNullOrEmpty, so null carries no NRE risk and avoids allocating a throwaway single-element list.")]
    public IList<ProductConfigurationSection> ConfigurationSections =>
        ConfigurationSection is null ? null : [ConfigurationSection];
}
