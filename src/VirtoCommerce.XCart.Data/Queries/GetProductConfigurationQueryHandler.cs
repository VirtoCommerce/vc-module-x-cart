using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCatalog.Data.Queries;

public class GetProductConfigurationQueryHandler : IQueryHandler<GetProductConfigurationQuery, ProductConfigurationQueryResponse>
{
    private readonly IConfigurableProductService _configurableProductService;
    private readonly IConfiguredLineItemContainerService _configuredLineItemContainerService;
    private readonly ICartProductService2 _cartProductService;

    public GetProductConfigurationQueryHandler(
        IConfigurableProductService configurableProductService,
        IConfiguredLineItemContainerService configuredLineItemContainerService,
        ICartProductService2 cartProductService)
    {
        _configurableProductService = configurableProductService;
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
    }

    public async Task<ProductConfigurationQueryResponse> Handle(GetProductConfigurationQuery request, CancellationToken cancellationToken)
    {
        var configuration = await _configurableProductService.GetProductConfigurationAsync(request.ConfigurableProductId);

        var containter = await _configuredLineItemContainerService.CreateContainerAsync(request);

        var allProductIds = configuration.ConfigurationSections.SelectMany(x => x.Options.Select(x => x.ProductId)).Distinct().ToArray();
        var cartProducts = await _cartProductService.GetCartProductsByIdsAsync(containter, allProductIds, loadPrice: true, loadInventory: true);

        var productByIds = cartProducts.ToDictionary(x => x.Product.Id, x => x);

        var result = new ProductConfigurationQueryResponse();
        foreach (var section in configuration.ConfigurationSections)
        {
            var configurationSection = new ExpProductConfigurationSection
            {
                Id = section.Id,
                Name = section.Name,
                IsRequired = section.IsRequired,
                Description = section.Description,
                Type = section.Type,
            };
            result.ConfigurationSections.Add(configurationSection);

            foreach (var option in section.Options)
            {
                if (productByIds.TryGetValue(option.ProductId, out var cartProduct))
                {
                    var item = containter.AddItem(cartProduct, option.Quantity);
                    item.Id = option.Id;

                    var expConfigurationLineItem = new ExpConfigurationLineItem
                    {
                        Item = item,
                        Currency = containter.Currency,
                        CultureName = containter.CultureName,
                        UserId = containter.UserId,
                        StoreId = containter.Store.Id,
                    };

                    configurationSection.Options.Add(expConfigurationLineItem);
                }
            }
        }

        return result;
    }
}
