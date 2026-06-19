using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared factory helpers for the checkout-cluster benchmarks
/// (shipments, payments, addresses). All fixtures follow the same design
/// rule as <see cref="CartBenchmarkFixtures"/>: I/O leaves are mocked,
/// pure compute (totals calculator) runs for real.
///
/// Harnesses here wrap <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> so they
/// share the same never-cache + fresh-cart-per-call idempotency guarantee.
/// LOCAL cart factories extend <see cref="CartBenchmarkFixtures.CreateCart"/> only when
/// the shared factory does not seed the domain collection the handler needs
/// (e.g. a pre-existing Shipment for RemoveShipment, a pre-existing Payment
/// for InitializeCartPayment).
/// </summary>
/// <remarks>
/// FLAG — shared-fixture limitation: <see cref="CartBenchmarkFixtures.CreateCart"/> seeds
/// <c>Shipments = []</c> and <c>Payments = []</c>. Handlers that require a pre-existing
/// Shipment or Payment call <see cref="CreateCartWithShipment"/> /
/// <see cref="CreateCartWithPayment"/> defined here to extend the base cart factory
/// locally rather than modifying the shared file.
/// </remarks>
internal static class CheckoutBenchmarkFixtures
{
    // ── shipment constants ───────────────────────────────────────────────────

    /// <summary>Shipment-method code used across all shipment benchmarks.</summary>
    public const string ShipmentMethodCode = "fixed-rate";

    /// <summary>Shipment-method option name used across all shipment benchmarks.</summary>
    public const string ShipmentMethodOption = "default";

    /// <summary>Shipment price — must match the rate returned by the mock avail-methods
    /// service exactly (the Strict validator rejects a price mismatch).</summary>
    public const decimal ShipmentPrice = 5m;

    /// <summary>Id of the pre-seeded shipment added by <see cref="CreateCartWithShipment"/>.</summary>
    public const string SeededShipmentId = "shipment-0";

    // ── payment constants ────────────────────────────────────────────────────

    /// <summary>Payment-gateway code used across all payment benchmarks.</summary>
    public const string PaymentGatewayCode = "bench-pay";

    /// <summary>Id of the pre-seeded payment added by <see cref="CreateCartWithPayment"/>.</summary>
    public const string SeededPaymentId = "payment-0";

    // ── shipment helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartShipment</c> command that adds a new shipment (no Id set →
    /// new-shipment path). The shipment method code/option/price match the mock available
    /// rate so the Strict <see cref="VirtoCommerce.XCart.Core.Validators.CartShipmentValidator"/>
    /// passes without throwing.
    /// </summary>
    public static AddOrUpdateCartShipmentCommand CreateAddOrUpdateCartShipmentCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddOrUpdateCartShipmentCommand
        {
            Shipment = new ExpCartShipment
            {
                // No Id → new-shipment branch (existingShipment == null)
                ShipmentMethodCode = new Optional<string>(ShipmentMethodCode),
                ShipmentMethodOption = new Optional<string>(ShipmentMethodOption),
                Price = new Optional<decimal>(ShipmentPrice),
                Currency = new Optional<string>(CartBenchmarkFixtures.Currency.Code),
            },
        });

    /// <summary>
    /// A <c>removeShipment</c> command targeting <see cref="SeededShipmentId"/> — the shipment
    /// pre-loaded by <see cref="CreateCartWithShipment"/>.
    /// </summary>
    public static RemoveShipmentCommand CreateRemoveShipmentCommand() =>
        CartBenchmarkFixtures.WithCartContext(new RemoveShipmentCommand { ShipmentId = SeededShipmentId });

    /// <summary>A <c>clearShipments</c> command targeting the benchmark cart.</summary>
    public static ClearShipmentsCommand CreateClearShipmentsCommand() =>
        CartBenchmarkFixtures.WithCartContext(new ClearShipmentsCommand());

    // ── payment helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartPayment</c> command that adds a new payment (no Id set →
    /// new-payment path). The gateway code matches the mock available method so the Strict
    /// <see cref="VirtoCommerce.XCart.Core.Validators.CartPaymentValidator"/> passes.
    /// </summary>
    public static AddOrUpdateCartPaymentCommand CreateAddOrUpdateCartPaymentCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddOrUpdateCartPaymentCommand
        {
            Payment = new ExpCartPayment
            {
                // No Id → new-payment branch (payment lookup returns null → MapTo(null))
                PaymentGatewayCode = new Optional<string>(PaymentGatewayCode),
                Currency = new Optional<string>(CartBenchmarkFixtures.Currency.Code),
                Amount = new Optional<decimal>(100m),
            },
        });

    /// <summary>A <c>clearPayments</c> command targeting the benchmark cart.</summary>
    public static ClearPaymentsCommand CreateClearPaymentsCommand() =>
        CartBenchmarkFixtures.WithCartContext(new ClearPaymentsCommand());

    // ── address helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartAddress</c> command setting a billing address keyed by
    /// <c>"bench-key"</c>. The handler does a plain list upsert — no validator that throws.
    /// </summary>
    public static AddOrUpdateCartAddressCommand CreateAddOrUpdateCartAddressCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddOrUpdateCartAddressCommand
        {
            Address = new ExpCartAddress
            {
                Key = new Optional<string>("bench-key"),
                FirstName = new Optional<string>("Bench"),
                LastName = new Optional<string>("User"),
                City = new Optional<string>("Seattle"),
                CountryCode = new Optional<string>("US"),
                CountryName = new Optional<string>("United States"),
                PostalCode = new Optional<string>("98101"),
                Line1 = new Optional<string>("123 Bench Ave"),
                AddressType = new Optional<int>((int)VirtoCommerce.CoreModule.Core.Common.AddressType.Billing),
            },
        });

    /// <summary>
    /// An <c>addCartAddress</c> command setting a shipping address by address-type.
    /// The handler uses <c>AddOrUpdateCartAddressByTypeAsync</c> — a distinct code path
    /// from <c>addOrUpdateCartAddress</c> which matches by Key.
    /// </summary>
    public static AddCartAddressCommand CreateAddCartAddressCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddCartAddressCommand
        {
            Address = new ExpCartAddress
            {
                FirstName = new Optional<string>("Bench"),
                LastName = new Optional<string>("User"),
                City = new Optional<string>("Seattle"),
                CountryCode = new Optional<string>("US"),
                CountryName = new Optional<string>("United States"),
                PostalCode = new Optional<string>("98101"),
                Line1 = new Optional<string>("123 Bench Ave"),
                AddressType = new Optional<int>((int)VirtoCommerce.CoreModule.Core.Common.AddressType.Shipping),
            },
        });

    // ── local cart factories ─────────────────────────────────────────────────

    /// <summary>
    /// Extends <see cref="CartBenchmarkFixtures.CreateCart"/> with a pre-seeded shipment
    /// so <c>RemoveShipmentCommandHandler</c> finds a target (it is a no-op on an empty list).
    ///
    /// FLAG — shared-fixture limitation: <c>CartBenchmarkFixtures.CreateCart</c> always
    /// returns <c>Shipments = []</c>. Adding a seeded shipment here rather than there to
    /// avoid breaking other cluster benchmarks that rely on empty-shipments semantics.
    /// </summary>
    public static ShoppingCart CreateCartWithShipment(int lineItemCount, CartShape shape)
    {
        var cart = CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
        cart.Shipments = new List<Shipment>
        {
            new()
            {
                Id = SeededShipmentId,
                ShipmentMethodCode = ShipmentMethodCode,
                ShipmentMethodOption = ShipmentMethodOption,
                Price = ShipmentPrice,
                Currency = CartBenchmarkFixtures.Currency.Code,
            },
        };

        return cart;
    }

    /// <summary>
    /// Extends <see cref="CartBenchmarkFixtures.CreateCart"/> with a pre-seeded payment and
    /// returns the cart so <c>InitializeCartPaymentCommandHandler</c> can look up the payment.
    ///
    /// FLAG — shared-fixture limitation: same as <see cref="CreateCartWithShipment"/> above.
    /// </summary>
    public static ShoppingCart CreateCartWithPayment(int lineItemCount, CartShape shape)
    {
        var cart = CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
        cart.Payments = new List<Payment>
        {
            new()
            {
                Id = SeededPaymentId,
                PaymentGatewayCode = PaymentGatewayCode,
                Currency = CartBenchmarkFixtures.Currency.Code,
                Amount = 100m,
            },
        };

        return cart;
    }

    // ── available-method mocks ───────────────────────────────────────────────

    /// <summary>
    /// Mock <see cref="ICartAvailMethodsService"/> that returns one <see cref="ShippingRate"/>
    /// matching <see cref="ShipmentMethodCode"/> / <see cref="ShipmentMethodOption"/> /
    /// <see cref="ShipmentPrice"/>. The Strict validator matches code + option + rate exactly.
    /// </summary>
    public static ICartAvailMethodsService ShipmentAvailMethodsService()
    {
        var mock = new Mock<ICartAvailMethodsService>();
        mock.Setup(x => x.GetAvailableShippingRatesAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync(new[]
            {
                new ShippingRate
                {
                    ShippingMethod = new BenchmarkShippingMethod(ShipmentMethodCode),
                    OptionName = ShipmentMethodOption,
                    Rate = ShipmentPrice,
                },
            });

        return mock.Object;
    }

    /// <summary>
    /// Mock <see cref="ICartAvailMethodsService"/> that returns one <see cref="PaymentMethod"/>
    /// matching <see cref="PaymentGatewayCode"/>. The Strict validator matches by code only.
    /// </summary>
    public static ICartAvailMethodsService PaymentAvailMethodsService()
    {
        var mock = new Mock<ICartAvailMethodsService>();
        mock.Setup(x => x.GetAvailablePaymentMethodsAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync(new PaymentMethod[] { new BenchmarkPaymentMethod(PaymentGatewayCode) });

        return mock.Object;
    }

    // ── handler factories ────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="AddOrUpdateCartShipmentCommandHandler"/> over the shared mutation harness.
    /// Cart is loaded as anonymous (member = null → <c>IsAnonymous = true</c>), so the
    /// customer-preference branch is skipped. The pickup-location service is a loose mock
    /// (no BOPIS request) and the customer-preference service is a loose mock.
    /// </summary>
    public static AddOrUpdateCartShipmentCommandHandler CreateAddOrUpdateCartShipmentHandler(
        int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new AddOrUpdateCartShipmentCommandHandler(
            harness.Repository,
            ShipmentAvailMethodsService(),
            Mock.Of<IPickupLocationService>(),      // not called — no PickupLocationId in command
            Mock.Of<ICustomerPreferenceService>()); // not called — cart is anonymous
    }

    /// <summary>
    /// Real <see cref="AddOrUpdateCartPaymentCommandHandler"/> over the shared mutation harness.
    /// </summary>
    public static AddOrUpdateCartPaymentCommandHandler CreateAddOrUpdateCartPaymentHandler(
        int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new AddOrUpdateCartPaymentCommandHandler(harness.Repository, PaymentAvailMethodsService());
    }

    /// <summary>
    /// Real <see cref="InitializeCartPaymentCommandHandler"/> for the success path.
    ///
    /// <c>InitializeCartPaymentCommandHandler</c> is NOT a <c>CartCommandHandler{TCommand}</c>
    /// — it calls <see cref="ICartAggregateRepository.GetCartByIdAsync"/> directly (not
    /// <c>GetOrCreateCartFromCommandAsync</c>) and returns
    /// <see cref="InitializeCartPaymentResult"/> rather than <see cref="CartAggregate"/>.
    /// The cart must carry a pre-seeded payment matching <see cref="SeededPaymentId"/> +
    /// <see cref="PaymentGatewayCode"/>.
    /// The available payment method must have <c>AllowCartPayment = true</c> (guarded in Handle).
    ///
    /// FLAG — shared-fixture limitation: <c>CreateMutationHarness</c> seeds <c>Payments = []</c>;
    /// this handler requires a pre-existing payment, so we build a dedicated
    /// <see cref="CartAggregateRepository"/> here whose GetAsync returns
    /// <see cref="CreateCartWithPayment"/> instead.
    /// </summary>
    public static InitializeCartPaymentCommandHandler CreateInitializeCartPaymentHandler(
        int lineItemCount, CartShape shape)
    {
        var repository = CreateRepositoryWithCart(lineItemCount, shape, CreateCartWithPayment);

        // Payment method: AllowCartPayment must be true (it is virtual — override in the stub).
        // ProcessPaymentAsync must return a successful result.
        var paymentMethod = new BenchmarkPaymentMethod(PaymentGatewayCode);
        paymentMethod.SetProcessPaymentResult(new ProcessPaymentRequestResult { IsSuccess = true });

        var availMethodsService = new Mock<ICartAvailMethodsService>();
        availMethodsService
            .Setup(x => x.GetAvailablePaymentMethodsAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync(new PaymentMethod[] { paymentMethod });

        return new InitializeCartPaymentCommandHandler(repository, availMethodsService.Object);
    }

    /// <summary>
    /// Real <see cref="RemoveShipmentCommandHandler"/> over a mutation harness whose
    /// GetAsync returns a cart pre-seeded with <see cref="SeededShipmentId"/>.
    ///
    /// FLAG — shared-fixture limitation: standard harness seeds <c>Shipments = []</c>;
    /// we build a dedicated repository here so GetAsync yields
    /// <see cref="CreateCartWithShipment"/> instead.
    /// </summary>
    public static RemoveShipmentCommandHandler CreateRemoveShipmentHandler(int lineItemCount, CartShape shape)
    {
        var repository = CreateRepositoryWithCart(lineItemCount, shape, CreateCartWithShipment);

        return new RemoveShipmentCommandHandler(repository);
    }

    /// <summary>
    /// Real <see cref="ClearShipmentsCommandHandler"/> over the shared mutation harness.
    /// Clear on an empty list is a valid success path (no exception); the benchmark measures
    /// the load + clear + save cycle regardless of how many shipments exist.
    /// </summary>
    public static ClearShipmentsCommandHandler CreateClearShipmentsHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new ClearShipmentsCommandHandler(harness.Repository);
    }

    /// <summary>
    /// Real <see cref="ClearPaymentsCommandHandler"/> over the shared mutation harness.
    /// Same semantics as <see cref="CreateClearShipmentsHandler"/>.
    /// </summary>
    public static ClearPaymentsCommandHandler CreateClearPaymentsHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new ClearPaymentsCommandHandler(harness.Repository);
    }

    /// <summary>
    /// Real <see cref="AddOrUpdateCartAddressCommandHandler"/> over the shared mutation harness.
    /// </summary>
    public static AddOrUpdateCartAddressCommandHandler CreateAddOrUpdateCartAddressHandler(
        int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new AddOrUpdateCartAddressCommandHandler(harness.Repository);
    }

    /// <summary>
    /// Real <see cref="AddCartAddressCommandHandler"/> over the shared mutation harness.
    /// Distinct from <see cref="AddOrUpdateCartAddressCommandHandler"/> — uses
    /// <c>AddOrUpdateCartAddressByTypeAsync</c> which matches by AddressType, not by Key.
    /// </summary>
    public static AddCartAddressCommandHandler CreateAddCartAddressHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        return new AddCartAddressCommandHandler(harness.Repository);
    }

    // ── InitializeCartPayment command ────────────────────────────────────────

    /// <summary>
    /// An <c>initializeCartPayment</c> command targeting the pre-seeded payment.
    /// <c>InitializeCartPaymentCommand</c> is NOT a <see cref="CartCommand"/> so it does
    /// not use <see cref="CartBenchmarkFixtures.WithCartContext{T}"/>.
    /// </summary>
    public static InitializeCartPaymentCommand CreateInitializeCartPaymentCommand() =>
        new()
        {
            CartId = "benchmark-cart",
            PaymentId = SeededPaymentId,
            StoreId = CartBenchmarkFixtures.StoreId,
            CultureName = "en-US",
        };

    // ── internal helpers ─────────────────────────────────────────────────────

    /// <summary>Builds a <see cref="CartAggregateRepository"/> whose GetAsync returns
    /// a fresh cart from <paramref name="cartFactory"/> on every call, using the
    /// never-cache mock so the real load+recalc runs each invocation.</summary>
    private static CartAggregateRepository CreateRepositoryWithCart(
        int lineItemCount, CartShape shape,
        Func<int, CartShape, ShoppingCart> cartFactory)
    {
        var mapper = CartBenchmarkFixtures.CreateMapper();
        var cartProductService = CartBenchmarkFixtures.CartProductServiceMock();

        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([CartBenchmarkFixtures.CreateStore()]);

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync())
            .ReturnsAsync([CartBenchmarkFixtures.Currency]);

        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => [cartFactory(lineItemCount, shape)]);

        return new CartAggregateRepository(
            cartAggregateFactory: () => CartBenchmarkFixtures.CreateAggregate(mapper, cartProductService.Object),
            shoppingCartSearchService: Mock.Of<IShoppingCartSearchService>(),
            shoppingCartService: shoppingCartService.Object,
            currencyService: currencyService.Object,
            memberResolver: Mock.Of<IMemberResolver>(),
            storeService: storeService.Object,
            cartProductsService: cartProductService.Object,
            platformMemoryCache: CartBenchmarkFixtures.NeverCacheMock().Object,
            fileUploadService: Mock.Of<IFileUploadService>());
    }
}
