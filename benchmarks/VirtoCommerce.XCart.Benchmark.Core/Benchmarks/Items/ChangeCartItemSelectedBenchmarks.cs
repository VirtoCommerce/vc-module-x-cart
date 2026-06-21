using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemSelected</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the mutate-existing-cart path — load (real build + recalc), toggle the first
/// line item's checkout selection, save (recalc again). Only the I/O leaves are mocked; the totals
/// calculator is real. The selection flag feeds the recalc's selected-items totals, so this also
/// exercises the totals path on a changed selection set. Idempotent without [IterationSetup] (fresh
/// cart per call). Flat vs Configured; count axis.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class ChangeCartItemSelectedBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCartItemSelectedCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateChangeCartItemSelectedCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemSelected() => _mediator.Send(_command);
}
