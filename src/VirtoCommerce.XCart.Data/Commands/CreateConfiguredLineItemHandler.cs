using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemHandler : IRequestHandler<CreateConfiguredLineItemCommand, ConfiguredLineItemAggregate>
{
    private readonly Func<ConfiguredLineItemAggregate> _configurableLineItemAggregateFactory;
    private readonly ICurrencyService _currencyService;
    private readonly IMemberResolver _memberResolver;
    private readonly IStoreService _storeService;
    private readonly ICartProductService _cartProductService;

    public CreateConfiguredLineItemHandler(
        Func<ConfiguredLineItemAggregate> configurableLineItemAggregateFactory,
       ICurrencyService currencyService,
       IMemberResolver memberResolver,
       IStoreService storeService,
       ICartProductService cartProductService)
    {
        _configurableLineItemAggregateFactory = configurableLineItemAggregateFactory;
        _currencyService = currencyService;
        _memberResolver = memberResolver;
        _storeService = storeService;
        _cartProductService = cartProductService;
    }

    public async Task<ConfiguredLineItemAggregate> Handle(CreateConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var cart = AbstractTypeFactory<ShoppingCart>.TryCreateInstance();
        cart.CustomerId = request.UserId;
        cart.OrganizationId = request.OrganizationId;
        cart.Name = "configuration";
        cart.Type = "configuration";
        cart.StoreId = request.StoreId;
        cart.LanguageCode = request.CultureName;
        cart.Currency = request.CurrencyCode;
        cart.Items = new List<LineItem>();
        cart.Shipments = new List<Shipment>();
        cart.Payments = new List<Payment>();
        cart.Addresses = new List<CartModule.Core.Model.Address>();
        cart.TaxDetails = new List<TaxDetail>();
        cart.Coupons = new List<string>();
        cart.Discounts = new List<Discount>();
        cart.DynamicProperties = new List<DynamicObjectProperty>();

        var storeLoadTask = _storeService.GetByIdAsync(cart.StoreId);
        var allCurrenciesLoadTask = _currencyService.GetAllCurrenciesAsync();

        await Task.WhenAll(storeLoadTask, allCurrenciesLoadTask);

        var store = storeLoadTask.Result;
        var allCurrencies = allCurrenciesLoadTask.Result;

        if (store == null)
        {
            throw new OperationCanceledException($"Store with id {cart.StoreId} not found");
        }

        if (string.IsNullOrEmpty(cart.Currency))
        {
            cart.Currency = store.DefaultCurrency;
        }

        var language = !string.IsNullOrEmpty(cart.LanguageCode) ? cart.LanguageCode : store.DefaultLanguage;
        var currency = allCurrencies.GetCurrencyForLanguage(cart.Currency, language);
        var member = await _memberResolver.ResolveMemberByIdAsync(cart.CustomerId);

        var aggregate = _configurableLineItemAggregateFactory();
        aggregate.GrabCart(cart, store, member, currency);

        var product = (await _cartProductService.GetCartProductsByIdsAsync(aggregate, new[] { request.ConfigurableProductId })).FirstOrDefault();
        if (product == null)
        {
            throw new OperationCanceledException($"Product with id {request.ConfigurableProductId} not found");
        }
        aggregate.ConfigurableProduct = product;

        // create line items by configuration sections
        aggregate.ValidationRuleSet = ["default"];
        var lineItems = request.ConfigurationSections
            .Where(x => x.Value != null)
            .Select(section => new NewCartItem(section.Value.ProductId, section.Value.Quantity))
            .ToList();

        await aggregate.AddItemsAsync(lineItems);

        await aggregate.RecalculateAsync();

        return aggregate;
    }
}
