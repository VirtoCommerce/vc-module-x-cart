using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addOrUpdateCartShipment</c> GraphQL mutation, resolved
/// through <see cref="IMediator"/>: the add-new-shipment path — load the cart (real build + recalc),
/// run shipment validation against the mocked available rates, add the shipment, save (recalc). The
/// <c>CartShipmentValidator</c> runs in Strict mode (ThrowOnFailures); the fixture supplies method code
/// + option + price that exactly match the mocked rate so the validator passes every invocation.
///
/// The customer-preference branch is <b>in</b> the measurement: <c>ShoppingCart.IsAnonymous</c> is a
/// plain bool that nothing here sets, so the handler's <c>!IsAnonymous</c> guard passes. It builds the
/// preference key and calls <c>LoadAddressFromPreferencesAsync</c>, which reads the loose
/// <c>ICustomerPreferenceService</c> mock (returns nothing → the shipment's delivery address is
/// cleared). The paired save is skipped — the fixture command carries neither a delivery address nor a
/// pickup location.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class AddOrUpdateCartShipmentBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddOrUpdateCartShipmentCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(
            LineItemCount,
            Shape,
            customizeServices: s => s.AddSingleton<ICartAvailMethodsService>(CheckoutBenchmarkFixtures.ShipmentAvailMethodsService()))
            .GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateAddOrUpdateCartShipmentCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddOrUpdateCartShipment() => _mediator.Send(_command);
}
