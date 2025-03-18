using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.Configuration;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands.Configuration;

public class CreateConfiguredLineItemHandler(
    IConfiguredLineItemContainerService configuredLineItemContainerService,
    ICartProductsLoaderService cartProductService,
    IFileUploadService fileUploadService)
    : ConfigurationCommandHandler<CreateConfiguredLineItemCommand>(configuredLineItemContainerService, cartProductService, fileUploadService)
{
    public override async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await ConfiguredLineItemContainerService.CreateContainerAsync(request);

        var productsRequest = container.GetCartProductsRequest();
        productsRequest.ProductIds = [request.ConfigurableProductId];
        productsRequest.EvaluatePromotions = request.EvaluatePromotions;

        var product = (await CartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();

        container.ConfigurableProduct = product ?? throw new InvalidOperationException($"Product with id {request.ConfigurableProductId} not found");

        // need to take productId and quantity from the configuration
        var selectedProductIds = request.ConfigurationSections
            .Where(x => x.Option != null)
            .Select(section => section.Option.ProductId)
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var products = await CartProductService.GetCartProductsAsync(productsRequest);

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
                var files = await CreateFiles(section);
                container.AddFileSectionLineItem(files, section.SectionId);
            }
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }
}
