using System.Collections.Generic;
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
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;
using CatalogProductConfigurationSection = VirtoCommerce.CatalogModule.Core.Model.Configuration.ProductConfigurationSection;

namespace VirtoCommerce.XCart.Data.Queries;

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
        var productsRequest = container.GetCartProductsRequest();

        productsRequest.ProductIds = configuration.Sections
            .SelectMany(x => x.Options?.Select(x => x.ProductId).Where(x => !string.IsNullOrEmpty(x)))
            .Distinct()
            .ToArray();

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
                AllowCustomText = section.AllowCustomText,
                AllowTextOptions = section.AllowPredefinedOptions,
            };

            result.ConfigurationSections.Add(configurationSection);

            AddProductOptions(section, configurationSection, container, productByIds);
            AddTextOptions(section, configurationSection);
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

    private static void AddProductOptions(CatalogProductConfigurationSection section, ExpProductConfigurationSection configurationSection, ConfiguredLineItemContainer container, Dictionary<string, CartProduct> productByIds)
    {
        if (section.Type == ConfigurationSectionTypeProduct && !section.Options.IsNullOrEmpty())
        {
            foreach (var option in section.Options)
            {
                if (productByIds.TryGetValue(option.ProductId, out var cartProduct))
                {
                    var item = container.CreateLineItem(cartProduct, option.Quantity);

                    var expConfigurationLineItem = new ExpProductConfigurationOption
                    {
                        Id = option.Id,
                        Quantity = option.Quantity,
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

    private static void AddTextOptions(CatalogProductConfigurationSection section, ExpProductConfigurationSection configurationSection)
    {
        if (section.Type == ConfigurationSectionTypeText && section.AllowPredefinedOptions && !section.Options.IsNullOrEmpty())
        {
            foreach (var option in section.Options)
            {
                var expConfigurationLineItem = new ExpProductConfigurationOption
                {
                    Id = option.Id,
                    Text = option.Text,
                };

                configurationSection.Options.Add(expConfigurationLineItem);
            }
        }
    }
}
