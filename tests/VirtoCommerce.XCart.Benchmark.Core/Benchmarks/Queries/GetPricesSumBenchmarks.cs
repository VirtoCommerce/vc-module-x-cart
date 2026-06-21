using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>getPricesSum</c> GraphQL query, resolved through
/// <see cref="IMediator"/>: a two-aggregate path. The measured compute: (1) load the source cart by
/// CartId (real load + recalc); (2) build a fresh temp aggregate (real build + recalc); (3) copy line
/// items source → temp; (4) <c>RecalculateAsync</c> on the temp; (5) read totals. Only I/O leaves are
/// mocked. Both builds run the real totals calculator — ~2× the recalc cost of <c>getCart</c> plus the
/// copy overhead. Two axes: shape (Flat vs Configured) and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Queries)]
public abstract class GetPricesSumBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private GetPricesSumQuery _query = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _query = ReadLoadBenchmarkFixtures.CreateGetPricesSumQuery(LineItemCount);
    }

    [Benchmark]
    public Task<ExpPricesSum> GetPricesSum() => _mediator.Send(_query);
}
