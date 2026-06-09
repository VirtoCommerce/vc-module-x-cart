using System;
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

        var responseGroup = GetResponseGroup(request);

        var configuration = await GetConfiguration(request, responseGroup);

        if (configuration is null)
        {
            return result;
        }

        // Narrow to the requested sections (when specified) so we build and enrich only what is needed.
        var sections = configuration.Sections.AsEnumerable();
        if (!request.SectionIds.IsNullOrEmpty())
        {
            sections = sections.Where(x => request.SectionIds.Contains(x.Id));
        }

        var orderedSections = sections.OrderBy(x => x.DisplayOrder).ToList();

        // Load and build options (and their products/prices) only when the client requested them.
        var loadOptions = responseGroup.HasFlag(ProductConfigurationResponseGroup.Options);

        ConfiguredLineItemContainer container = null;
        var productByIds = new Dictionary<string, CartProduct>();

        if (loadOptions)
        {
            container = await _configuredLineItemContainerService.CreateContainerAsync(request);
            var productsRequest = container.GetCartProductsRequest();

            productsRequest.ProductIds = orderedSections
                .SelectMany(x => x.Options?.Select(o => o.ProductId).Where(id => !string.IsNullOrEmpty(id)) ?? [])
                .Distinct()
                .ToArray();

            var cartProducts = await _cartProductService.GetCartProductsAsync(productsRequest);
            productByIds = cartProducts.ToDictionary(x => x.Product.Id, x => x);
        }

        foreach (var section in orderedSections)
        {
            var configurationSection = CreateConfigurationSection(section);

            result.ConfigurationSections.Add(configurationSection);

            if (loadOptions)
            {
                AddProductOptions(section, configurationSection, container, productByIds);
                AddTextOptions(section, configurationSection);
            }
        }

        return result;
    }

    protected virtual ProductConfigurationResponseGroup GetResponseGroup(GetProductConfigurationQuery request)
    {
        // No field selection provided (e.g. the standalone product configuration query) → load the full graph.
        if (request.IncludeFields.IsNullOrEmpty())
        {
            return ProductConfigurationResponseGroup.Full;
        }

        var optionFields = request.IncludeFields
            .Where(x => x.Contains("options", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (optionFields.Count == 0)
        {
            return ProductConfigurationResponseGroup.Sections;
        }

        // Option sub-fields that reference a product / price / image require option-referenced products to be loaded.
        var needsProducts = optionFields.Any(x =>
            x.Contains("product", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("price", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("image", StringComparison.OrdinalIgnoreCase));

        return needsProducts
            ? ProductConfigurationResponseGroup.Full
            : ProductConfigurationResponseGroup.Options;
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
        result.DependsOnSectionId = section.DependsOnSectionId;

        return result;
    }

    protected virtual async Task<ProductConfiguration> GetConfiguration(GetProductConfigurationQuery request, ProductConfigurationResponseGroup responseGroup)
    {
        var criteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
        criteria.ProductId = request.ConfigurableProductId;
        criteria.IsActive = true;
        criteria.ResponseGroup = responseGroup.ToString();

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

                    var expConfigurationLineItem = AbstractTypeFactory<ExpConfigurationLineItem>.TryCreateInstance();
                    expConfigurationLineItem.Id = option.Id;
                    expConfigurationLineItem.IsDefault = option.IsDefault;
                    expConfigurationLineItem.Quantity = option.Quantity;
                    expConfigurationLineItem.Item = item;
                    expConfigurationLineItem.Currency = container.Currency;
                    expConfigurationLineItem.CultureName = container.CultureName;
                    expConfigurationLineItem.UserId = container.UserId;
                    expConfigurationLineItem.StoreId = container.Store.Id;

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
                var expConfigurationLineItem = AbstractTypeFactory<ExpConfigurationLineItem>.TryCreateInstance();
                expConfigurationLineItem.Id = option.Id;
                expConfigurationLineItem.IsDefault = option.IsDefault;
                expConfigurationLineItem.Text = option.Text;

                configurationSection.Options.Add(expConfigurationLineItem);
            }
        }
    }
}
