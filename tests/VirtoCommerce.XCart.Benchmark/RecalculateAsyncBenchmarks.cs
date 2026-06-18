using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Moq;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Standalone microbenchmark of <see cref="CartAggregate.RecalculateAsync"/> — the cart hot path
/// (fires on every read via the aggregate cache and inside every mutation's save) run against the
/// <b>real</b> <c>DefaultShoppingCartTotalsCalculator</c>. This is the finest-grained gate for
/// totals-math allocation/throughput regressions.
///
/// No method-level baseline: the operations are not alternatives, so a within-run <c>Ratio</c>
/// would compare nothing. The comparison axes are <b>scale</b> (the count param rows) and
/// <b>before/after</b> a change (see README). The count axis includes 100 as the superlinearity
/// canary — RecalculateAsync is one of the operations where O(n²) is plausible.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(BenchmarkCategories.Tier1)]
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
