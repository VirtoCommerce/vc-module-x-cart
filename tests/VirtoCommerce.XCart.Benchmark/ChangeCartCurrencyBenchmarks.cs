using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartCurrency</c> GraphQL mutation
/// (<see cref="ChangeCartCurrencyCommandHandler.Handle"/>): the re-price path. Measured compute =
/// load primary cart (real <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/>
/// build + recalc), load or create the new-currency cart aggregate, copy all items (flat branch:
/// <c>AddItemsAsync</c>; configured branch: full <c>ConfiguredLineItemContainer</c> re-price per
/// item), save (recalc again). Only I/O leaves are mocked; the totals calculator is real.
///
/// <b>Currency choice</b>: <c>NewCurrencyCode = "USD"</c> (same as the loaded cart). The shared
/// currency service mock returns only USD, so any other code would fail to find the currency and
/// throw during re-price. A same-currency switch still exercises the full <c>CopyItems</c> code
/// path — both the flat and configured branches — making it a valid success-path measure.
///
/// Two axes: <b>Shape</b> (Flat vs Configured — configured triggers the heavier
/// <c>CopyConfiguredItems</c> branch with per-item product re-price) and <b>LineItemCount</b>
/// (100 surfaces super-linear growth in the copy loop + recalc walk).
///
/// Idempotent without [IterationSetup]: the repository's GetAsync mock returns a fresh cart per
/// call, so currency-change never accumulates across invocations.
/// </summary>
[MemoryDiagnoser]
public class ChangeCartCurrencyBenchmarks
{
    private ChangeCartCurrencyCommandHandler _handler = null!;
    private readonly ChangeCartCurrencyCommand _command = CartStateBenchmarkFixtures.CreateChangeCartCurrencyCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartStateBenchmarkFixtures.CreateChangeCartCurrencyHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangeCartCurrency() => _handler.Handle(_command, CancellationToken.None);
}
