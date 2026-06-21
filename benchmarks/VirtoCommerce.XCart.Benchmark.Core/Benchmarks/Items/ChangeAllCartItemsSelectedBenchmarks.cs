using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeAllCartItemsSelected</c> GraphQL mutation, resolved
/// through <see cref="IMediator"/>: load (real build + recalc), toggle EVERY line item's checkout
/// selection, save (recalc again). The handler feeds all of the cart's line item ids into
/// <c>CartAggregate.ChangeItemsSelectedAsync</c>, whose per-id <c>Items.FirstOrDefault</c> lookup makes
/// the bulk selection update O(N²) in cart size — the path the singular <c>changeCartItemSelected</c>
/// (one id, a near-pure recalc envelope) never exercises. Read the <c>Allocated</c>/time columns across
/// the count axis to watch the quadratic. Only the I/O leaves are mocked; the totals calculator is real.
/// Idempotent without [IterationSetup] (fresh cart per call).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class ChangeAllCartItemsSelectedBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeAllCartItemsSelectedCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateChangeAllCartItemsSelectedCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeAllCartItemsSelected() => _mediator.Send(_command);
}
