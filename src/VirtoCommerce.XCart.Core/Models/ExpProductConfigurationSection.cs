using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class ExpProductConfigurationSection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public string Type { get; set; }
    public bool AllowCustomText { get; set; }
    public bool AllowTextOptions { get; set; }

    public IList<ExpProductConfigurationOption> Options { get; set; } = [];
}
