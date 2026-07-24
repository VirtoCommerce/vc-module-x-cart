using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared factory helpers for the checkout-cluster benchmarks
/// (shipments, payments, addresses). All fixtures follow the same design
/// rule as <see cref="CartBenchmarkFixtures"/>: I/O leaves are mocked,
/// pure compute (totals calculator) runs for real.
///
/// Command builders use <see cref="AbstractTypeFactory{T}"/> so an override registered by the
/// module setup is honored, then wrap the command via
/// <see cref="CartBenchmarkFixtures.WithCartContext{T}"/> where the command is a
/// <see cref="VirtoCommerce.XCart.Core.Commands.BaseCommands.CartCommand"/>.
///
/// Per-op cart state (a pre-existing shipment / payment) is seeded through the
/// <c>customizeCart</c> hook on <see cref="CartBenchmarkBase.BuildProvider"/> using
/// <see cref="SeedShipment"/> / <see cref="SeedPayment"/>; avail-method scenario leaves are
/// overridden through the <c>customizeServices</c> hook using
/// <see cref="ShipmentAvailMethodsService"/> / <see cref="PaymentAvailMethodsService"/>.
/// </summary>
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

    /// <summary>Id of the pre-seeded shipment added by <see cref="SeedShipment"/>.</summary>
    public const string SeededShipmentId = "shipment-0";

    // ── payment constants ────────────────────────────────────────────────────

    /// <summary>Payment-gateway code used across all payment benchmarks.</summary>
    public const string PaymentGatewayCode = "bench-pay";

    /// <summary>Id of the pre-seeded payment added by <see cref="SeedPayment"/>.</summary>
    public const string SeededPaymentId = "payment-0";

    // ── shipment helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartShipment</c> command that adds a new shipment (no Id set →
    /// new-shipment path). The shipment method code/option/price match the mock available
    /// rate so the Strict <see cref="VirtoCommerce.XCart.Core.Validators.CartShipmentValidator"/>
    /// passes without throwing.
    /// </summary>
    public static AddOrUpdateCartShipmentCommand CreateAddOrUpdateCartShipmentCommand()
    {
        var command = AbstractTypeFactory<AddOrUpdateCartShipmentCommand>.TryCreateInstance();
        command.Shipment = new ExpCartShipment
        {
            // No Id → new-shipment branch (existingShipment == null)
            ShipmentMethodCode = new Optional<string>(ShipmentMethodCode),
            ShipmentMethodOption = new Optional<string>(ShipmentMethodOption),
            Price = new Optional<decimal>(ShipmentPrice),
            Currency = new Optional<string>(CartBenchmarkFixtures.Currency.Code),
        };

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>
    /// A <c>removeShipment</c> command targeting <see cref="SeededShipmentId"/> — the shipment
    /// pre-loaded by <see cref="SeedShipment"/>.
    /// </summary>
    public static RemoveShipmentCommand CreateRemoveShipmentCommand()
    {
        var command = AbstractTypeFactory<RemoveShipmentCommand>.TryCreateInstance();
        command.ShipmentId = SeededShipmentId;

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>A <c>clearShipments</c> command targeting the benchmark cart.</summary>
    public static ClearShipmentsCommand CreateClearShipmentsCommand()
    {
        var command = AbstractTypeFactory<ClearShipmentsCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── payment helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartPayment</c> command that adds a new payment (no Id set →
    /// new-payment path). The gateway code matches the mock available method so the Strict
    /// <see cref="VirtoCommerce.XCart.Core.Validators.CartPaymentValidator"/> passes.
    /// </summary>
    public static AddOrUpdateCartPaymentCommand CreateAddOrUpdateCartPaymentCommand()
    {
        var command = AbstractTypeFactory<AddOrUpdateCartPaymentCommand>.TryCreateInstance();
        command.Payment = new ExpCartPayment
        {
            // No Id → new-payment branch (payment lookup returns null → MapTo(null))
            PaymentGatewayCode = new Optional<string>(PaymentGatewayCode),
            Currency = new Optional<string>(CartBenchmarkFixtures.Currency.Code),
            Amount = new Optional<decimal>(100m),
        };

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>A <c>clearPayments</c> command targeting the benchmark cart.</summary>
    public static ClearPaymentsCommand CreateClearPaymentsCommand()
    {
        var command = AbstractTypeFactory<ClearPaymentsCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── address helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// An <c>addOrUpdateCartAddress</c> command setting a billing address keyed by
    /// <c>"bench-key"</c>. The handler does a plain list upsert — no validator that throws.
    /// </summary>
    public static AddOrUpdateCartAddressCommand CreateAddOrUpdateCartAddressCommand()
    {
        var command = AbstractTypeFactory<AddOrUpdateCartAddressCommand>.TryCreateInstance();
        command.Address = new ExpCartAddress
        {
            Key = new Optional<string>("bench-key"),
            FirstName = new Optional<string>("Bench"),
            LastName = new Optional<string>("User"),
            City = new Optional<string>("Seattle"),
            CountryCode = new Optional<string>("US"),
            CountryName = new Optional<string>("United States"),
            PostalCode = new Optional<string>("98101"),
            Line1 = new Optional<string>("123 Bench Ave"),
            AddressType = new Optional<int>((int)AddressType.Billing),
        };

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>
    /// An <c>addCartAddress</c> command setting a shipping address by address-type.
    /// The handler uses <c>AddOrUpdateCartAddressByTypeAsync</c> — a distinct code path
    /// from <c>addOrUpdateCartAddress</c> which matches by Key.
    /// </summary>
    public static AddCartAddressCommand CreateAddCartAddressCommand()
    {
        var command = AbstractTypeFactory<AddCartAddressCommand>.TryCreateInstance();
        command.Address = new ExpCartAddress
        {
            FirstName = new Optional<string>("Bench"),
            LastName = new Optional<string>("User"),
            City = new Optional<string>("Seattle"),
            CountryCode = new Optional<string>("US"),
            CountryName = new Optional<string>("United States"),
            PostalCode = new Optional<string>("98101"),
            Line1 = new Optional<string>("123 Bench Ave"),
            AddressType = new Optional<int>((int)AddressType.Shipping),
        };

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── cart-state seed hooks (customizeCart) ────────────────────────────────

    /// <summary>
    /// Seeds the cart with a single shipment matching <see cref="SeededShipmentId"/> so
    /// <c>RemoveShipmentCommandHandler</c> finds a target (it is a no-op on an empty list).
    /// Used as the <c>customizeCart</c> hook for the remove-shipment benchmark.
    /// </summary>
    public static void SeedShipment(ShoppingCart cart) =>
        cart.Shipments =
        [
            new Shipment
            {
                Id = SeededShipmentId,
                ShipmentMethodCode = ShipmentMethodCode,
                ShipmentMethodOption = ShipmentMethodOption,
                Price = ShipmentPrice,
                Currency = CartBenchmarkFixtures.Currency.Code,
            },
        ];

    /// <summary>
    /// Seeds the cart with a single payment matching <see cref="SeededPaymentId"/> so
    /// <c>InitializeCartPaymentCommandHandler</c> can look up the payment.
    /// Used as the <c>customizeCart</c> hook for the initialize-cart-payment benchmark.
    /// </summary>
    public static void SeedPayment(ShoppingCart cart) =>
        cart.Payments =
        [
            new Payment
            {
                Id = SeededPaymentId,
                PaymentGatewayCode = PaymentGatewayCode,
                Currency = CartBenchmarkFixtures.Currency.Code,
                Amount = 100m,
            },
        ];

    // ── available-method mocks (customizeServices) ───────────────────────────

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
    /// The <see cref="BenchmarkPaymentMethod"/> defaults to <c>AllowCartPayment = true</c> and a
    /// successful <c>ProcessPaymentAsync</c> result, so it also satisfies the initialize-cart-payment
    /// success path.
    /// </summary>
    public static ICartAvailMethodsService PaymentAvailMethodsService()
    {
        var mock = new Mock<ICartAvailMethodsService>();
        mock.Setup(x => x.GetAvailablePaymentMethodsAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync(new PaymentMethod[] { new BenchmarkPaymentMethod(PaymentGatewayCode) });

        return mock.Object;
    }

    // ── InitializeCartPayment command ────────────────────────────────────────

    /// <summary>
    /// An <c>initializeCartPayment</c> command targeting the pre-seeded payment.
    /// <c>InitializeCartPaymentCommand</c> is NOT a <see cref="VirtoCommerce.XCart.Core.Commands.BaseCommands.CartCommand"/>
    /// so it does not use <see cref="CartBenchmarkFixtures.WithCartContext{T}"/>.
    /// </summary>
    public static InitializeCartPaymentCommand CreateInitializeCartPaymentCommand()
    {
        var command = AbstractTypeFactory<InitializeCartPaymentCommand>.TryCreateInstance();
        command.CartId = "benchmark-cart";
        command.PaymentId = SeededPaymentId;
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CultureName = "en-US";

        return command;
    }
}
