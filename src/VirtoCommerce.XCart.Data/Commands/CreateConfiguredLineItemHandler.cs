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
    private readonly ICartProductService2 _cartProductService;

    public CreateConfiguredLineItemHandler(
       IConfiguredLineItemContainerService configuredLineItemContainerService,
       ICartProductService2 cartProductService)
    {
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
    }

    public async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await _configuredLineItemContainerService.CreateContainerAsync(request);

        var product = (await _cartProductService.GetCartProductsByIdsAsync(container, new[] { request.ConfigurableProductId })).FirstOrDefault();
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

        var products = await _cartProductService.GetCartProductsByIdsAsync(container, selectedProductIds, loadPrice: true, loadInventory: false);

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
