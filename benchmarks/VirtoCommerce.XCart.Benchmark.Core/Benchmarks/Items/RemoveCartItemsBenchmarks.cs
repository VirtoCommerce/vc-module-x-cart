using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeCartItems</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: load (real build + recalc), remove EVERY line item, save (recalc over the
/// now-empty set). <c>CartAggregate.RemoveItemsAsync</c> resolves the id list with a
/// <c>Items.Where(ids.Contains)</c> scan plus a per-item <c>List.Remove</c>, so removing all N items is
/// O(N²) in cart size — the bulk-remove path the singular <c>removeCartItem</c> (one id) never
/// exercises. Read the count axis to watch the quadratic. Only the I/O leaves are mocked; the totals
/// calculator is real. Idempotent without [IterationSetup] (fresh cart per call — the removal never
/// accumulates).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class RemoveCartItemsBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveCartItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateRemoveCartItemsCommand(LineItemCount);
    }

    [Benchmark]
    public Task<CartAggregate> RemoveCartItems() => _mediator.Send(_command);
}
