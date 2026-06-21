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
/// + option + price that exactly match the mocked rate so the validator passes every invocation. Cart
/// is anonymous (member = null), so the customer-preference branch is skipped and only the core
/// shipment-add + recalc is measured.
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart (Shipments = []) per
/// call and the never-cache forces a real load every invocation. Two axes: shape (Flat vs Configured)
/// and cart size — surfaces recalc super-linear growth and configured-product regressions.
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
