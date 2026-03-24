using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
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
        productsRequest.OrganizationId = request.OrganizationId;

        var product = (await _cartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();

        container.ConfigurableProduct = product ?? throw new InvalidOperationException($"Product with id {request.ConfigurableProductId} not found");

        // need to take productId and quantity from the configuration
        var configurationSections = request.ConfigurationSections ?? [];

        var selectedProductIds = configurationSections
            .Select(x => x.Option?.ProductId)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var products = await _cartProductService.GetCartProductsAsync(productsRequest);

        foreach (var section in configurationSections)
        {
            if (section.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation && section.Option != null)
            {
                var productOption = section.Option;
                var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId) ?? throw new InvalidOperationException($"Product with id {productOption.ProductId} not found");

                var configurationItem = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();
                configurationItem.ProductId = productOption.ProductId;
                configurationItem.Quantity = productOption.Quantity;
                configurationItem.SectionId = section.SectionId;
                configurationItem.Type = section.Type;
                configurationItem.SelectedForCheckout = productOption.SelectedForCheckout;

                container.AddProductSectionLineItem(selectedProduct, configurationItem);
            }
            else if (section.Type == ConfigurationSectionTypeText)
            {
                container.AddTextSectionLineItem(section.CustomText, section.SectionId);
            }
            else if (section.Type == ConfigurationSectionTypeFile)
            {
                var files = await CreateConfigurationFiles(section, request.CartId);
                container.AddFileSectionLineItem(files, section.SectionId);
            }
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }

    private async Task<IList<ConfigurationItemFile>> CreateConfigurationFiles(ProductConfigurationSection section, string cartId)
    {
        if (section.FileUrls.IsNullOrEmpty())
        {
            return null;
        }

        return (await _fileUploadService.GetByPublicUrlAsync(section.FileUrls))
            .Where(x => x.Scope == ConfigurationSectionFilesScope && (x.OwnerIsEmpty() || x.OwnerIs<ShoppingCart>(cartId)))
            .Select(x => x.ConvertToConfigurationItemFile())
            .ToList();
    }
}
