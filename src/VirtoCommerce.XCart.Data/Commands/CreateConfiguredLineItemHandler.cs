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

        var product = (await _cartProductService.GetCartProductsByIdsAsync(productsRequest)).FirstOrDefault();
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
        var products = await _cartProductService.GetCartProductsByIdsAsync(productsRequest);

        foreach (var section in request.ConfigurationSections)
        {
            var productOption = section.Value;
            var selectedProduct = products.FirstOrDefault(x => x.Product.Id == productOption.ProductId);
            if (selectedProduct == null)
            {
                throw new OperationCanceledException($"Product with id {productOption.ProductId} not found");
            }

            _ = container.AddItem(selectedProduct, productOption.Quantity);
        }

        var configuredItem = container.CreateConfiguredLineItem();

        return configuredItem;
    }
}
