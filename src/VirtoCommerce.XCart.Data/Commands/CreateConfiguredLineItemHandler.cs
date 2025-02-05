using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemHandler : IRequestHandler<CreateConfiguredLineItemCommand, ExpConfigurationLineItem>
{
    private readonly IConfiguredLineItemContainerService _configuredLineItemContainerService;
    private readonly ICartProductsLoaderService _cartProductService;

    public CreateConfiguredLineItemHandler(
       IConfiguredLineItemContainerService configuredLineItemContainerService,
       ICartProductsLoaderService cartProductService)
    {
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
    }

    public async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await _configuredLineItemContainerService.CreateContainerAsync(request);

        var productsRequest = container.GetCartProductsRequest();
        productsRequest.ProductIds = [request.ConfigurableProductId];
        productsRequest.EvaluatePromotions = request.EvaluatePromotions;

        var product = (await _cartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();

        container.ConfigurableProduct = product ?? throw new OperationCanceledException($"Product with id {request.ConfigurableProductId} not found");

        foreach (var section in request.ConfigurationSections)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (section.Value != null && section.Option == null)
            {
                section.Option = section.Value;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

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
            if (section.Type == ConfigurationSectionType.Product && section.Option != null)
            {
                var productOption = section.Option;
                var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId) ?? throw new OperationCanceledException($"Product with id {productOption.ProductId} not found");

                container.AddItem(selectedProduct, productOption.Quantity, section.SectionId, section.Type);
            }

            if (section.Type == ConfigurationSectionType.Text)
            {
                container.AddItem(section.CustomText, section.SectionId, section.Type);
            }
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }
}
