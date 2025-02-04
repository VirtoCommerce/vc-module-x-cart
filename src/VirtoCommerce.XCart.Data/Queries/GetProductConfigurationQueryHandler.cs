using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCatalog.Data.Queries;

public class GetProductConfigurationQueryHandler : IQueryHandler<GetProductConfigurationQuery, ProductConfigurationQueryResponse>
{
    private readonly IProductConfigurationSearchService _productConfigurationSearchService;
    private readonly IConfiguredLineItemContainerService _configuredLineItemContainerService;
    private readonly ICartProductsLoaderService _cartProductService;

    public GetProductConfigurationQueryHandler(
        IProductConfigurationSearchService productConfigurationSearchService,
        IConfiguredLineItemContainerService configuredLineItemContainerService,
        ICartProductsLoaderService cartProductService)
    {
        _productConfigurationSearchService = productConfigurationSearchService;
        _configuredLineItemContainerService = configuredLineItemContainerService;
        _cartProductService = cartProductService;
    }

    public async Task<ProductConfigurationQueryResponse> Handle(GetProductConfigurationQuery request, CancellationToken cancellationToken)
    {
        var result = new ProductConfigurationQueryResponse();

        var configuration = await GetConfiguration(request);

        if (configuration == null)
        {
            return result;
        }

        var container = await _configuredLineItemContainerService.CreateContainerAsync(request);

        var allProductIds = configuration.Sections.SelectMany(x => x.Options?.Select(x => x.ProductId)).Distinct().ToArray();

        var productsRequest = container.GetCartProductsRequest();
        productsRequest.ProductIds = allProductIds;
        var cartProducts = await _cartProductService.GetCartProductsAsync(productsRequest);

        var productByIds = cartProducts.ToDictionary(x => x.Product.Id, x => x);

        foreach (var section in configuration.Sections.OrderBy(x => x.DisplayOrder))
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

            if (section.Type == ProductConfigurationSectionType.Product && !section.Options.IsNullOrEmpty())
            {
                foreach (var option in section.Options)
                {
                    if (productByIds.TryGetValue(option.ProductId, out var cartProduct))
                    {
                        var item = container.AddItem(cartProduct, option.Quantity, section.Id);
                        item.Id = option.Id;

                        var expConfigurationLineItem = new ExpConfigurationLineItem
                        {
                            Item = item,
                            Currency = container.Currency,
                            CultureName = container.CultureName,
                            UserId = container.UserId,
                            StoreId = container.Store.Id,
                        };

                        configurationSection.Options.Add(expConfigurationLineItem);
                    }
                }
            }
        }

        return result;
    }

    private async Task<ProductConfiguration> GetConfiguration(GetProductConfigurationQuery request)
    {
        var criteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
        criteria.ProductId = request.ConfigurableProductId;

        var configurationsResult = await _productConfigurationSearchService.SearchNoCloneAsync(criteria);
        return configurationsResult.Results.FirstOrDefault();
    }
}
