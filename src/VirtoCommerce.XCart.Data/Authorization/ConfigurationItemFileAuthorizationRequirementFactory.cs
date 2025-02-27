using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Models;

namespace VirtoCommerce.XCart.Data.Authorization;
public class ConfigurationItemFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public string Scope => CatalogModule.Core.ModuleConstants.ConfigurationSectionFilesScope;

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new CanAccessCartAuthorizationRequirement();
    }
}
