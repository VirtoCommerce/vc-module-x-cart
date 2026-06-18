using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addOrUpdateCartShipment</c> GraphQL mutation
/// (<see cref="AddOrUpdateCartShipmentCommandHandler.Handle"/>): the add-new-shipment path — load the
/// cart (real build + recalc), run shipment validation against the mocked available rates, add the
/// shipment, save (recalc). The <c>CartShipmentValidator</c> runs in Strict mode (ThrowOnFailures); the
/// fixture supplies method code + option + price that exactly match the mocked rate so the validator
/// passes every invocation. Cart is anonymous (member = null), so the customer-preference branch is
/// skipped and only the core shipment-add + recalc is measured.
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart (Shipments = []) per
/// call and the never-cache forces a real load every invocation. Two axes: shape (Flat vs Configured)
/// and cart size — surfaces recalc super-linear growth and configured-product regressions.
/// </summary>
[MemoryDiagnoser]
public class AddOrUpdateCartShipmentBenchmarks
{
    private AddOrUpdateCartShipmentCommandHandler _handler = null!;
    private readonly AddOrUpdateCartShipmentCommand _command =
        CheckoutBenchmarkFixtures.CreateAddOrUpdateCartShipmentCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateAddOrUpdateCartShipmentHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> AddOrUpdateCartShipment() =>
        _handler.Handle(_command, CancellationToken.None);
}
