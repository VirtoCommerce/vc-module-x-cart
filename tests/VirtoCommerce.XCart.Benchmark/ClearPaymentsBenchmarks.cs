using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>clearPayments</c> GraphQL mutation
/// (<see cref="ClearPaymentsCommandHandler.Handle"/>): load the cart (real build + recalc),
/// call <c>ClearPaymentsAsync</c> (sets Payments = []), save (recalc).
///
/// Same semantics as <see cref="ClearShipmentsBenchmarks"/>: the load + empty-collection-assign +
/// save + recalc cycle is measured on a cart with empty Payments. The full item graph walk (load
/// recalc + save recalc) is still the dominant cost. Two axes: shape and cart size.
/// </summary>
[MemoryDiagnoser]
public class ClearPaymentsBenchmarks
{
    private ClearPaymentsCommandHandler _handler = null!;
    private readonly ClearPaymentsCommand _command =
        CheckoutBenchmarkFixtures.CreateClearPaymentsCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateClearPaymentsHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ClearPayments() =>
        _handler.Handle(_command, CancellationToken.None);
}
