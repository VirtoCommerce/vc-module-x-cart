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
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Extensions;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemHandler : IRequestHandler<CreateConfiguredLineItemCommand, ExpConfigurationLineItem>
{
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
            .Where(x => x.Option != null && !string.IsNullOrEmpty(x.Option.ProductId))
            .Select(section => section.Option.ProductId)
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var products = await _cartProductService.GetCartProductsAsync(productsRequest);

        foreach (var section in request.ConfigurationSections)
        {
            if (section.Type == ConfigurationSectionTypeProduct && section.Option != null)
            {
                var productOption = section.Option;
                var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId) ?? throw new InvalidOperationException($"Product with id {productOption.ProductId} not found");

                container.AddProductSectionLineItem(selectedProduct, productOption.Quantity, section.SectionId);
            }

            if (section.Type == ConfigurationSectionTypeText)
            {
                container.AddTextSectionLIneItem(section.CustomText, section.SectionId);
            }

            if (section.Type == ConfigurationSectionTypeFile)
            {
                var files = await CreateFiles(section, request.CartId);
                container.AddFileSectionLineItem(files, section.SectionId);
            }
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }

    protected virtual async Task<IList<ConfigurationItemFile>> CreateFiles(ProductConfigurationSection section, string cartId)
    {
        var filesByUrls = (await _fileUploadService.GetByPublicUrlAsync(section.FileUrls))
            .Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIsEmpty() || (x.OwnerEntityId == cartId && x.OwnerEntityType == typeof(ShoppingCart).FullName))
            .ToDictionary(x => x.PublicUrl, StringComparer.OrdinalIgnoreCase);

        var configurationItemFiles = new List<ConfigurationItemFile>(section.FileUrls.Count);

        foreach (var url in section.FileUrls)
        {
            if (filesByUrls.TryGetValue(url, out var file))
            {
                configurationItemFiles.Add(file.ConvertToItemFile());
            }
        }

        return configurationItemFiles;
    }
}
