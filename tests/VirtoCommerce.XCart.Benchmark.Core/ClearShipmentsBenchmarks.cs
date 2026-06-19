using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>clearShipments</c> GraphQL mutation
/// (<see cref="ClearShipmentsCommandHandler.Handle"/>): load the cart (real build + recalc),
/// call <c>ClearShipmentsAsync</c> (sets Shipments = []), save (recalc).
///
/// Measuring on a cart with empty Shipments is valid — the load + empty-collection-assign + save +
/// recalc cycle is what the benchmark captures regardless of initial shipment count. The measured
/// compute still walks the full cart item graph twice (load recalc + save recalc).
///
/// Idempotent without [IterationSetup]: GetAsync mock returns a fresh cart per call. Two axes.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public class ClearShipmentsBenchmarks
{
    private ClearShipmentsCommandHandler _handler = null!;
    private readonly ClearShipmentsCommand _command =
        CheckoutBenchmarkFixtures.CreateClearShipmentsCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateClearShipmentsHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ClearShipments() =>
        _handler.Handle(_command, CancellationToken.None);
}
