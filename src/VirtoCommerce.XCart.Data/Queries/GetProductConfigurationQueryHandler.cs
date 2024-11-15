using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCatalog.Data.Queries;

public class GetProductConfigurationQueryHandler : IQueryHandler<GetProductConfigurationQuery, ProductConfigurationQueryResponse>
{
    private readonly Func<ConfiguredLineItemContainer> _configurableLineItemAggregateFactory;
    private readonly IConfigurableProductService _configurableProductService;
    private readonly ICurrencyService _currencyService;
    private readonly IMemberResolver _memberResolver;
    private readonly IStoreService _storeService;
    private readonly ICartProductService2 _cartProductService;
    //private readonly IMediator _mediator;

    private const string _productsFieldName = $"{nameof(ProductConfigurationQueryResponse.ConfigurationSections)}.{nameof(ExpProductConfigurationSection.Options)}";

    public GetProductConfigurationQueryHandler(
        Func<ConfiguredLineItemContainer> configurableLineItemContainerFactory,
        IConfigurableProductService configurableProductService,
        ICurrencyService currencyService,
        IMemberResolver memberResolver,
        IStoreService storeService,
        ICartProductService2 cartProductService)
    {
        _configurableLineItemAggregateFactory = configurableLineItemContainerFactory;
        _configurableProductService = configurableProductService;
        _currencyService = currencyService;
        _memberResolver = memberResolver;
        _storeService = storeService;
        _cartProductService = cartProductService;
    }

    public async Task<ProductConfigurationQueryResponse> Handle(GetProductConfigurationQuery request, CancellationToken cancellationToken)
    {
        var configuration = await _configurableProductService.GetProductConfigurationAsync(request.ConfigurableProductId);

        var containter = await CreateContainer(request);

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
                    var item = containter.CreateItem(cartProduct, option.Quantity);
                    item.Id = option.Id;

                    var expConfigurationLineItem = new ExpConfigurationLineItem
                    {
                        Item = item,
                        Currency = containter.Currency,
                        Product = cartProduct.ExpProduct,
                    };

                    configurationSection.Options.Add(expConfigurationLineItem);
                }
            }
        }

        return result;
    }

    // todo: move to the separate service
    private async Task<ConfiguredLineItemContainer> CreateContainer(GetProductConfigurationQuery request)
    {
        var storeLoadTask = _storeService.GetByIdAsync(request.StoreId);
        var allCurrenciesLoadTask = _currencyService.GetAllCurrenciesAsync();
        await Task.WhenAll(storeLoadTask, allCurrenciesLoadTask);

        var store = storeLoadTask.Result;
        var allCurrencies = allCurrenciesLoadTask.Result;

        if (store == null)
        {
            throw new OperationCanceledException($"Store with id {request.StoreId} not found");
        }

        var language = !string.IsNullOrEmpty(request.CultureName) ? request.CultureName : store.DefaultLanguage;
        var currencyCode = !string.IsNullOrEmpty(request.CurrencyCode) ? request.CurrencyCode : store.DefaultCurrency;
        var currency = allCurrencies.GetCurrencyForLanguage(currencyCode, language);

        var member = await _memberResolver.ResolveMemberByIdAsync(request.UserId);

        var container = _configurableLineItemAggregateFactory();

        container.Store = store;
        container.Member = member;
        container.Currency = currency;
        container.CultureName = language;
        container.UserId = request.UserId;
        container.OrganizationId = request.OrganizationId;

        return container;
    }
}
