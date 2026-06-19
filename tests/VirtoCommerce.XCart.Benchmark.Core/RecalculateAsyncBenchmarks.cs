using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Moq;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Standalone microbenchmark of <see cref="CartAggregate.RecalculateAsync"/> — the cart hot path
/// (fires on every read via the aggregate cache and inside every mutation's save) run against the
/// <b>real</b> <c>DefaultShoppingCartTotalsCalculator</c>. The finest-grained probe for totals-math
/// allocation/throughput regressions.
///
/// No method-level baseline: the operations are not alternatives, so a within-run <c>Ratio</c>
/// would compare nothing. Read the <c>Allocated</c> column across the count/shape rows (scale) and
/// before/after a change. The count axis includes 100 to surface super-linear (O(n²)) growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Recalculate)]
public class RecalculateAsyncBenchmarks
{
    private CartAggregate _aggregate = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // RecalculateAsync never maps, so a mock mapper is fine here.
        _aggregate = CartBenchmarkFixtures.CreateAggregate(Mock.Of<AutoMapper.IMapper>());

        var cart = CartBenchmarkFixtures.CreateCart(LineItemCount, Shape);
        _aggregate.GrabCart(cart, CartBenchmarkFixtures.CreateStore(), member: null, CartBenchmarkFixtures.Currency);
    }

    // RecalculateAsync is idempotent (running it twice produces the same state), so no
    // [IterationSetup] reset is needed — BDN can invoke it freely.
    [Benchmark]
    public Task<CartAggregate> RecalculateAsync() => _aggregate.RecalculateAsync();
}
