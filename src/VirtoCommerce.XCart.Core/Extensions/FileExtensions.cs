using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Core.Extensions;

public static class FileExtensions
{
    public static ConfigurationItemFile ConvertToConfigurationItemFile(this File file)
    {
        var configurationItemFile = AbstractTypeFactory<ConfigurationItemFile>.TryCreateInstance();

        configurationItemFile.Name = file.Name;
        configurationItemFile.ContentType = file.ContentType;
        configurationItemFile.Size = file.Size;
        configurationItemFile.Url = file.PublicUrl;

        return configurationItemFile;
    }

    public static IList<string> GetConfigurationFileUrls(this LineItem lineItem)
    {
        return lineItem.ConfigurationItems
            ?.Where(x => x.Files != null)
            .SelectMany(x => x.Files)
            .Select(x => x.Url)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList() ?? [];
    }
}
