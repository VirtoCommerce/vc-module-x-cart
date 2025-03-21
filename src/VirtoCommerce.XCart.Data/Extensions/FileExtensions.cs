using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Data.Extensions;

public static class FileExtensions
{
    public static ConfigurationItemFile ConvertToItemFile(this File file)
    {
        var configurationItemFile = AbstractTypeFactory<ConfigurationItemFile>.TryCreateInstance();

        configurationItemFile.Name = file.Name;
        configurationItemFile.ContentType = file.ContentType;
        configurationItemFile.Size = file.Size;
        configurationItemFile.Url = file.PublicUrl;

        return configurationItemFile;
    }
}
