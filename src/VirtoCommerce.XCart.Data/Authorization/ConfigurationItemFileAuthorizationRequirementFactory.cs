using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Models;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Authorization;

public class ConfigurationItemFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public string Scope => ConfigurationSectionFilesScope;

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new CanAccessCartAuthorizationRequirement();
    }
}
