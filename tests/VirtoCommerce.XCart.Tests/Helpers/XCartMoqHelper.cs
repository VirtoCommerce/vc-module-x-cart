using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoFixture;
using AutoMapper;
using Bogus;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CartModule.Data.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.InventoryModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers.Stubs;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;

namespace VirtoCommerce.XCart.Tests.Helpers
{
    public class XCartMoqHelper : BaseMoqHelper
    {
        // For Validators
        protected readonly CartValidationContext _context = new CartValidationContext();

        protected readonly Mock<ICartProductService> _cartProductServiceMock;
        protected readonly Mock<ICurrencyService> _currencyServiceMock;
        protected readonly Mock<IMarketingPromoEvaluator> _marketingPromoEvaluatorMock;
        protected readonly Mock<IPaymentMethodsSearchService> _paymentMethodsSearchServiceMock;
        protected readonly Mock<IShippingMethodsSearchService> _shippingMethodsSearchServiceMock;
        protected readonly Mock<IShoppingCartTotalsCalculator> _shoppingCartTotalsCalculatorMock;
        protected readonly Mock<IStoreService> _crudStoreServiceMock;
        protected readonly Mock<IOptionalDependency<ITaxProviderSearchService>> _taxProviderSearchServiceMock;
        protected readonly Mock<IDynamicPropertyUpdaterService> _dynamicPropertyUpdaterService;
        protected readonly Mock<IMapper> _mapperMock;
        protected readonly Mock<IMemberService> _memberService;
        protected readonly Mock<IGenericPipelineLauncher> _genericPipelineLauncherMock;
        protected readonly Mock<IConfigurationItemValidator> _configurationItemValidatorMock;
        protected readonly Mock<IFileUploadService> _fileUploadService;

        protected readonly Randomizer Rand = new Randomizer();

        private const string CART_NAME = "default";

        protected const int InStockQuantity = 100;
        protected const int ItemCost = 50;

        public XCartMoqHelper()
        {
            _fixture.Register<PaymentMethod>(() => new StubPaymentMethod(_fixture.Create<string>()));

            _fixture.Register(() => _fixture
                .Build<ShoppingCart>()
                .With(x => x.Currency, CURRENCY_CODE)
                .With(x => x.LanguageCode, CULTURE_NAME)
                .With(x => x.Name, CART_NAME)
                .Without(x => x.Items)
                .Create());

            _fixture.Register(() =>
            {
                var catalogProduct = _fixture.Create<CatalogProduct>();

                catalogProduct.TrackInventory = true;

                var cartProduct = new CartProduct(catalogProduct);

                cartProduct.ApplyPrices(new List<Price>()
                {
                    new Price
                    {
                        ProductId = catalogProduct.Id,
                        PricelistId = _fixture.Create<string>(),
                        List = ItemCost,
                        MinQuantity = 1,
                    }
                }, GetCurrency());

                var store = GetStore();

                cartProduct.ApplyInventories(new List<InventoryInfo>()
                {
                    new InventoryInfo
                    {
                        ProductId=catalogProduct.Id,
                        FulfillmentCenterId = store.MainFulfillmentCenterId,
                        InStockQuantity = InStockQuantity,
                        ReservedQuantity = 0,
                    }
                }, store);

                return cartProduct;
            });

            _fixture.Register(() => new CatalogProduct
            {
                Id = _fixture.Create<string>(),
                IsActive = true,
                IsBuyable = true,
            });

            _fixture.Register(() => _fixture.Build<LineItem>()
                                            .Without(x => x.DynamicProperties)
                                            .With(x => x.IsReadOnly, false)
                                            .With(x => x.IsGift, false)
                                            .With(x => x.Quantity, InStockQuantity)
                                            .With(x => x.SalePrice, ItemCost)
                                            .With(x => x.ListPrice, ItemCost)
                                            .Create());

            _fixture.Register<Price>(() => null);

            _fixture.Register(() =>
                _fixture.Build<Optional<string>>()
                .With(x => x.IsSpecified, true)
                .Create());

            _fixture.Register(() =>
                _fixture.Build<Optional<int>>()
                .With(x => x.IsSpecified, true)
                .Create());

            _fixture.Register(() =>
                _fixture.Build<Optional<decimal>>()
                .With(x => x.IsSpecified, true)
                .Create());

            _fixture.Register(() =>
                _fixture.Build<Optional<decimal?>>()
                .With(x => x.IsSpecified, true)
                .Create());

            _fixture.Register(() =>
                _fixture.Build<Optional<ExpCartAddress>>()
               .With(x => x.IsSpecified, true)
               .Create());

            _cartProductServiceMock = new Mock<ICartProductService>();

            _currencyServiceMock = new Mock<ICurrencyService>();
            _currencyServiceMock
                .Setup(x => x.GetAllCurrenciesAsync())
                .ReturnsAsync(_fixture.CreateMany<Currency>(1).ToList());

            _marketingPromoEvaluatorMock = new Mock<IMarketingPromoEvaluator>();
            _marketingPromoEvaluatorMock
                .Setup(x => x.EvaluatePromotionAsync(It.IsAny<IEvaluationContext>()))
                .ReturnsAsync(new StubPromotionResult());

            _paymentMethodsSearchServiceMock = new Mock<IPaymentMethodsSearchService>();
            _shippingMethodsSearchServiceMock = new Mock<IShippingMethodsSearchService>();
            _shoppingCartTotalsCalculatorMock = new Mock<IShoppingCartTotalsCalculator>();

            _crudStoreServiceMock = new Mock<IStoreService>();
            _crudStoreServiceMock
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(_fixture.CreateMany<Store>(1).ToList);

            _taxProviderSearchServiceMock = new Mock<IOptionalDependency<ITaxProviderSearchService>>();
            _dynamicPropertyUpdaterService = new Mock<IDynamicPropertyUpdaterService>();

            _mapperMock = new Mock<IMapper>();

            _genericPipelineLauncherMock = new Mock<IGenericPipelineLauncher>();

            _memberService = new Mock<IMemberService>();
            _memberService
                .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_fixture.Create<Organization>());

            _configurationItemValidatorMock = new Mock<IConfigurationItemValidator>();
            _configurationItemValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<LineItem>(), CancellationToken.None))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _fileUploadService = new Mock<IFileUploadService>();
            _fileUploadService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(() => []);
        }

        protected ShoppingCart GetCart() => _fixture.Create<ShoppingCart>();

        protected Member GetMember() => _fixture.Create<StubMember>();

        protected Store GetStore() => _fixture.Create<Store>();

        protected NewCartItem BuildNewCartItem(
            string productId,
            int quantity,
            decimal productPrice,
            bool? isActive = null,
            bool? isBuyable = null,
            bool? trackInventory = null)
        {
            var catalogProductId = _fixture.Create<string>();

            var catalogProduct = new CatalogProduct
            {
                Id = catalogProductId,
                IsActive = isActive,
                IsBuyable = isBuyable,
                TrackInventory = trackInventory
            };

            var cartProduct = new CartProduct(catalogProduct);
            cartProduct.ApplyPrices(new List<Price>()
            {
                new Price
                {
                    ProductId = catalogProductId,
                    PricelistId = _fixture.Create<string>(),
                    List = _fixture.Create<decimal>(),
                    MinQuantity = _fixture.Create<int>(),
                }
            }, GetCurrency());

            var newCartItem = new NewCartItem(productId, quantity)
            {
                Price = productPrice,
                CartProduct = cartProduct
            };

            return newCartItem;
        }

        protected CartAggregate GetValidAnonymousCartAggregate(ShoppingCart cart = null, Currency currency = null)
        {
            var aggregate = new CartAggregate(
                _marketingPromoEvaluatorMock.Object,
                GeTotalsCalculator(currency),
                _taxProviderSearchServiceMock.Object,
                _cartProductServiceMock.Object,
                _dynamicPropertyUpdaterService.Object,
                _mapperMock.Object,
                _memberService.Object,
                _genericPipelineLauncherMock.Object,
                _configurationItemValidatorMock.Object,
                _fileUploadService.Object);

            aggregate.GrabCart(cart ?? GetCart(), new Store(), null, currency ?? GetCurrency());

            return aggregate;
        }

        protected CartAggregate GetValidCartAggregate(ShoppingCart cart = null, Currency currency = null)
        {
            var aggregate = new CartAggregate(
                _marketingPromoEvaluatorMock.Object,
                GeTotalsCalculator(currency),
                _taxProviderSearchServiceMock.Object,
                _cartProductServiceMock.Object,
                _dynamicPropertyUpdaterService.Object,
                _mapperMock.Object,
                _memberService.Object,
                _genericPipelineLauncherMock.Object,
                _configurationItemValidatorMock.Object,
                _fileUploadService.Object);

            aggregate.GrabCart(cart ?? GetCart(), new Store(), GetMember(), currency ?? GetCurrency());

            return aggregate;
        }

        private IShoppingCartTotalsCalculator GeTotalsCalculator(Currency currency)
        {
            IShoppingCartTotalsCalculator totalsCalculator;

            if (currency != null)
            {
                var currencyServiceMock = new Mock<ICurrencyService>();

                currencyServiceMock
                    .Setup(x => x.GetAllCurrenciesAsync())
                    .ReturnsAsync([currency]);

                totalsCalculator = new DefaultShoppingCartTotalsCalculator(currencyServiceMock.Object);
            }
            else
            {
                totalsCalculator = _shoppingCartTotalsCalculatorMock.Object;
            }

            return totalsCalculator;
        }
    }
}
