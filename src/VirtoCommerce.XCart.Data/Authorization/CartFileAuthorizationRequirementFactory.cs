using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Authorization;

public class CartFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public bool CanCreateRequirement(File file)
    {
        return file.Scope.EqualsIgnoreCase(ConfigurationSectionFilesScope) && file.OwnerTypeIs<ShoppingCart>();
    }

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new CanAccessCartAuthorizationRequirement();
    }
}
