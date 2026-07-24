using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <b>cart-level</b> <c>changeComment</c> GraphQL mutation
/// (<see cref="ChangeCommentCommandHandler.Handle"/>): sets the cart's top-level comment string.
/// Measured compute = load cart (real build + recalc), call <c>UpdateCartComment</c> (sets the
/// cart-level comment string), save (recalc again). Only I/O leaves are mocked; the totals
/// calculator is real.
///
/// <b>Distinct from <see cref="ChangeCartItemCommentBenchmarks"/></b>: that benchmark covers the
/// <em>per-item</em> comment mutation (<c>ChangeCartItemCommentCommandHandler</c>); this one covers
/// the <em>cart-level</em> comment (<c>ChangeCommentCommandHandler</c>) which mutates
/// <c>CartAggregate.Cart.Comment</c> directly without an item lookup.
///
/// Like <see cref="ChangePurchaseOrderNumberBenchmarks"/>, this is a scalar mutation; the measured
/// time is dominated by the two recalc passes. Useful as a minimum-cost reference.
///
/// Two axes: <b>Shape</b> (Flat vs Configured — diverges on recalc cost) and <b>LineItemCount</b>.
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public abstract class ChangeCommentBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCommentCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartStateBenchmarkFixtures.CreateChangeCommentCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeComment() => _mediator.Send(_command);
}
