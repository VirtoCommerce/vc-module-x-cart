using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.Configuration;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands.Configuration;

public abstract class ConfigurationCommandHandler<TConfigurationCommand>(
    IConfiguredLineItemContainerService configuredLineItemContainerService,
    ICartProductsLoaderService cartProductService,
    IFileUploadService fileUploadService)
    : IRequestHandler<TConfigurationCommand, ExpConfigurationLineItem> where TConfigurationCommand : ConfiguredLineItemCommand
{
    protected IConfiguredLineItemContainerService ConfiguredLineItemContainerService { get; private set; } = configuredLineItemContainerService;
    protected ICartProductsLoaderService CartProductService { get; private set; } = cartProductService;
    protected IFileUploadService FileUploadService { get; private set; } = fileUploadService;

    public abstract Task<ExpConfigurationLineItem> Handle(TConfigurationCommand request, CancellationToken cancellationToken);

    protected virtual async Task<IList<ConfigurationItemFile>> CreateFiles(ProductConfigurationSection section, ConfigurationItem configurationItem = null)
    {
        var filesByUrls = (await FileUploadService.GetByPublicUrlAsync(section.FileUrls))
            .Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIsEmpty() || x.OwnerIs(configurationItem))
            .ToDictionary(x => x.PublicUrl, StringComparer.OrdinalIgnoreCase);

        var configurationItemFiles = new List<ConfigurationItemFile>(section.FileUrls.Count);

        foreach (var url in section.FileUrls)
        {
            if (filesByUrls.TryGetValue(url, out var file))
            {
                configurationItemFiles.Add(ConvertToItemFile(file));
            }
        }

        return configurationItemFiles;
    }

    protected virtual ConfigurationItemFile ConvertToItemFile(File file)
    {
        var configurationItemFile = AbstractTypeFactory<ConfigurationItemFile>.TryCreateInstance();

        configurationItemFile.Name = file.Name;
        configurationItemFile.ContentType = file.ContentType;
        configurationItemFile.Size = file.Size;
        configurationItemFile.Url = file.PublicUrl;

        return configurationItemFile;
    }
}
