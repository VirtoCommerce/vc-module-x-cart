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

    public virtual async Task<ProductConfigurationQueryResponse> Handle(GetProductConfigurationQuery request, CancellationToken cancellationToken)
    {
        var result = AbstractTypeFactory<ProductConfigurationQueryResponse>.TryCreateInstance();

        var configuration = await GetConfiguration(request);

        if (configuration is null)
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
            var configurationSection = CreateConfigurationSection(section);

            result.ConfigurationSections.Add(configurationSection);

            AddProductOptions(section, configurationSection, container, productByIds);
            AddTextOptions(section, configurationSection);
        }

        return result;
    }

    protected virtual ExpProductConfigurationSection CreateConfigurationSection(CatalogProductConfigurationSection section)
    {
        var result = AbstractTypeFactory<ExpProductConfigurationSection>.TryCreateInstance();

        result.Id = section.Id;
        result.Name = section.Name;
        result.IsRequired = section.IsRequired;
        result.Description = section.Description;
        result.Type = section.Type;
        result.AllowCustomText = section.AllowCustomText;
        result.AllowTextOptions = section.AllowPredefinedOptions;
        result.MaxLength = section.MaxLength;

        return result;
    }

    protected virtual async Task<ProductConfiguration> GetConfiguration(GetProductConfigurationQuery request)
    {
        var criteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
        criteria.ProductId = request.ConfigurableProductId;

        var configurationsResult = await _productConfigurationSearchService.SearchNoCloneAsync(criteria);
        return configurationsResult.Results.FirstOrDefault();
    }

    protected virtual void AddProductOptions(CatalogProductConfigurationSection section, ExpProductConfigurationSection configurationSection, ConfiguredLineItemContainer container, Dictionary<string, CartProduct> productByIds)
    {
        if (section.Type == ConfigurationSectionTypeProduct && !section.Options.IsNullOrEmpty())
        {
            foreach (var option in section.Options)
            {
                if (productByIds.TryGetValue(option.ProductId, out var cartProduct))
                {
                    var item = container.CreateLineItem(cartProduct, option.Quantity);

                    var expConfigurationLineItem = new ExpConfigurationLineItem
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

    protected virtual void AddTextOptions(CatalogProductConfigurationSection section, ExpProductConfigurationSection configurationSection)
    {
        if (section.Type == ConfigurationSectionTypeText && section.AllowPredefinedOptions && !section.Options.IsNullOrEmpty())
        {
            foreach (var option in section.Options)
            {
                var expConfigurationLineItem = new ExpConfigurationLineItem
                {
                    Id = option.Id,
                    Text = option.Text,
                };

                configurationSection.Options.Add(expConfigurationLineItem);
            }
        }
    }
}
