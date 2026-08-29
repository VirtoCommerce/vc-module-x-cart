using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>getCart</c> GraphQL query, resolved through
/// <see cref="IMediator"/>: the full load + recalc path. The measured compute = look up the cart by
/// CartId, build the aggregate (real <c>CartAggregateRepository</c>), run the real
/// <c>DefaultShoppingCartTotalsCalculator</c>.
///
/// The Configured shape additionally resolves each item's variation products on load.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Queries)]
public abstract class GetCartBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private GetCartQuery _query = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _query = ReadLoadBenchmarkFixtures.CreateGetCartQuery();
    }

    [Benchmark]
    public Task<CartAggregate> GetCart() => _mediator.Send(_query);
}
