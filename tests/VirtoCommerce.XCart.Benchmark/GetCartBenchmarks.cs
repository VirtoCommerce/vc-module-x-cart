using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>getCart</c> GraphQL query
/// (<see cref="GetCartQueryHandler.Handle(GetCartQuery, CancellationToken)"/>): the full
/// load + recalc path. The measured compute = look up the cart by CartId, build the aggregate
/// (real <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/>), run the real
/// <c>DefaultShoppingCartTotalsCalculator</c>, save the result into the aggregate; only I/O leaves
/// are mocked (DB read, never-cache).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call and the
/// never-cache forces a real load+recalc every invocation, so the handler sees a pristine state.
/// That keeps InvocationCount unforced and Mean precise.
///
/// Two axes: <b>shape</b> (Flat vs Configured — the configured cart's load path additionally
/// resolves variation products, giving a wider recalc surface) and cart size (100 surfaces
/// super-linear growth).
/// </summary>
[MemoryDiagnoser]
public class GetCartBenchmarks
{
    private GetCartQueryHandler _handler = null!;
    private readonly GetCartQuery _query = ReadLoadBenchmarkFixtures.CreateGetCartQuery();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = ReadLoadBenchmarkFixtures.CreateGetCartHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> GetCart() => _handler.Handle(_query, CancellationToken.None);
}
