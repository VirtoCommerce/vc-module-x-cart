using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemHandler : IRequestHandler<CreateConfiguredLineItemCommand, ExpConfigurationLineItem>
{
    private readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    private readonly IConfiguredLineItemContainerService _configuredLineItemContainerService;
    private readonly ICartProductsLoaderService _cartProductService;
    private readonly IFileUploadService _fileUploadService;

    public CreateConfiguredLineItemHandler(
       IConfiguredLineItemContainerService configuredLineItemContainerService,
       ICartProductsLoaderService cartProductService,
       IFileUploadService fileUploadService)
    {
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
        _fileUploadService = fileUploadService;
    }

    public async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await _configuredLineItemContainerService.CreateContainerAsync(request);

        var productsRequest = container.GetCartProductsRequest();
        productsRequest.ProductIds = [request.ConfigurableProductId];
        productsRequest.EvaluatePromotions = request.EvaluatePromotions;

        var product = (await _cartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();

        container.ConfigurableProduct = product ?? throw new InvalidOperationException($"Product with id {request.ConfigurableProductId} not found");

        // need to take productId and quantity from the configuration
        var selectedProductIds = request.ConfigurationSections
            .Where(x => x.Option != null)
            .Select(section => section.Option.ProductId)
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var products = await _cartProductService.GetCartProductsAsync(productsRequest);

        foreach (var section in request.ConfigurationSections)
        {
            if (section.Type == CatalogModule.Core.ModuleConstants.ConfigurationSectionTypeProduct && section.Option != null)
            {
                var productOption = section.Option;
                var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId) ?? throw new InvalidOperationException($"Product with id {productOption.ProductId} not found");

                container.AddProductSectionLineItem(selectedProduct, productOption.Quantity, section.SectionId);
            }

            if (section.Type == CatalogModule.Core.ModuleConstants.ConfigurationSectionTypeText)
            {
                container.AddTextSectionLIneItem(section.CustomText, section.SectionId);
            }

            if (section.Type == CatalogModule.Core.ModuleConstants.ConfigurationSectionTypeFile)
            {
                var files = await AddFiles(section);
                container.AddFiletSectionLIneItem(files, section.SectionId);
            }
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }

    protected virtual async Task<IList<ConfigurationItemFile>> AddFiles(ProductConfigurationSection request)
    {
        var ids = request.FileUrls
            .Select(FileExtensions.GetFileId)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var files = await _fileUploadService.GetAsync(ids);

        var filesByUrls = files
            .Where(x => x.Scope == CatalogModule.Core.ModuleConstants.ConfigurationSectionFilesScope && string.IsNullOrEmpty(x.OwnerEntityId) && string.IsNullOrEmpty(x.OwnerEntityType))
            .ToDictionary(x => FileExtensions.GetFileUrl(x.Id), _ignoreCase);

        var configurationItemFiles = new List<ConfigurationItemFile>(request.FileUrls.Count);

        foreach (var url in request.FileUrls)
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
        configurationItemFile.Url = FileExtensions.GetFileUrl(file.Id);

        return configurationItemFile;
    }
}
