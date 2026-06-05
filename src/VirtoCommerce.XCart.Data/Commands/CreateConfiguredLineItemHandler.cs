using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Search;
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
    private readonly IProductConfigurationSearchService _productConfigurationSearchService;

    public CreateConfiguredLineItemHandler(
       IConfiguredLineItemContainerService configuredLineItemContainerService,
       ICartProductsLoaderService cartProductService,
       IFileUploadService fileUploadService,
       IProductConfigurationSearchService productConfigurationSearchService)
    {
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
        _fileUploadService = fileUploadService;
        _productConfigurationSearchService = productConfigurationSearchService;
    }

    public virtual async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await _configuredLineItemContainerService.CreateContainerAsync(request);
        var productsRequest = container.GetCartProductsRequest();

        productsRequest.ProductIds = [request.ConfigurableProductId];
        productsRequest.EvaluatePromotions = request.EvaluatePromotions;
        productsRequest.OrganizationId = request.OrganizationId;
        productsRequest.ProductsIncludeFields = request.ProductsIncludeFields;

        var product = (await _cartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();

        container.ConfigurableProduct = product ?? throw new InvalidOperationException($"Product with id {request.ConfigurableProductId} not found");

        // need to take productId and quantity from the configuration
        var configurationSections = request.ConfigurationSections ?? [];

        // Enrich each section with its catalog name; the container stamps it onto each created
        // ConfigurationItem so the persisted SectionName snapshot needs no catalog round-trip on load.
        await _productConfigurationSearchService.UpdateSectionsFromCatalogAsync(request.ConfigurableProductId, configurationSections);

        var selectedProductIds = configurationSections
            .Select(x => x.Option?.ProductId)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var cartProducts = (await _cartProductService.GetCartProductsAsync(productsRequest)).ToDictionary(x => x.Id);

        foreach (var section in configurationSections)
        {
            switch (section.Type)
            {
                case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation when !string.IsNullOrEmpty(section.Option?.ProductId):
                    var productOption = section.Option;
                    var cartProduct = cartProducts.GetValueOrDefault(productOption.ProductId) ?? throw new InvalidOperationException($"Product with id {productOption.ProductId} not found");

                    container.AddProductSectionLineItem(cartProduct, productOption.Quantity, productOption.SelectedForCheckout, section.SectionId, section.SectionName, section.Type);
                    break;

                case ConfigurationSectionTypeText:
                    container.AddTextSectionLineItem(section.CustomText, section.SectionId, section.SectionName);
                    break;

                case ConfigurationSectionTypeFile:
                    var files = await CreateConfigurationFiles(section, request.CartId);
                    container.AddFileSectionLineItem(files, section.SectionId, section.SectionName);
                    break;
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
