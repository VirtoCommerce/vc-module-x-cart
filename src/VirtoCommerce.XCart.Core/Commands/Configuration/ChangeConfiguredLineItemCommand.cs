using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Commands.Configuration;

public class ChangeConfiguredLineItemCommand : ConfiguredLineItemCommand
{
    public IList<ConfigurationItem> ConfigurationItems { get; set; } = [];
}
