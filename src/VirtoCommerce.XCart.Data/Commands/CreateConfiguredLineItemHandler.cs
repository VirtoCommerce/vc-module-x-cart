using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        productsRequest.ProductIds = new[] { request.ConfigurableProductId };
        productsRequest.EvaluatePromotions = request.EvaluatePromotions;

        var product = (await _cartProductService.GetCartProductsAsync(productsRequest)).FirstOrDefault();
        if (product == null)
        {
            throw new OperationCanceledException($"Product with id {request.ConfigurableProductId} not found");
        }

        container.ConfigurableProduct = product;

        // need to take productId and quantity from the configuration
        var selectedProductIds = request.ConfigurationSections
            .Where(x => x.Value != null)
            .Select(section => section.Value.ProductId)
            .ToList();

        productsRequest.ProductIds = selectedProductIds;
        productsRequest.LoadInventory = false;
        productsRequest.EvaluatePromotions = false; // don't need to evaluate promotions for the selected products

        var products = await _cartProductService.GetCartProductsAsync(productsRequest);

        foreach (var section in request.ConfigurationSections)
        {
            var productOption = section.Value;
            var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId);
            if (selectedProduct == null)
            {
                throw new OperationCanceledException($"Product with id {productOption.ProductId} not found");
            }

            _ = container.AddItem(selectedProduct, productOption.Quantity, section.SectionId);
        }

        var configuredItem = container.CreateConfiguredLineItem(request.Quantity);

        return configuredItem;
    }
}
