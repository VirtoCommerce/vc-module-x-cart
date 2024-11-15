using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemHandler : IRequestHandler<CreateConfiguredLineItemCommand, ExpConfigurationLineItem>
{
    private readonly Func<ConfiguredLineItemContainer> _configurableLineItemAggregateFactory;
    private readonly ICurrencyService _currencyService;
    private readonly IMemberResolver _memberResolver;
    private readonly IStoreService _storeService;
    private readonly ICartProductService2 _cartProductService;

    public CreateConfiguredLineItemHandler(
       Func<ConfiguredLineItemContainer> configurableLineItemContainerFactory,
       ICurrencyService currencyService,
       IMemberResolver memberResolver,
       IStoreService storeService,
       ICartProductService2 cartProductService)
    {
        _configurableLineItemAggregateFactory = configurableLineItemContainerFactory;
        _currencyService = currencyService;
        _memberResolver = memberResolver;
        _storeService = storeService;
        _cartProductService = cartProductService;
    }

    public async Task<ExpConfigurationLineItem> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var container = await CreateContainer(request);

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

            var item = container.CreateItem(selectedProduct, productOption.Quantity);
            container.Items.Add(item);
        }

        var configuredItem = container.CreateConfiguredLineItem();

        return new ExpConfigurationLineItem
        {
            Currency = container.Currency,
            Item = configuredItem
        };
    }

    // todo: move to the separate service
    private async Task<ConfiguredLineItemContainer> CreateContainer(CreateConfiguredLineItemCommand request)
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
