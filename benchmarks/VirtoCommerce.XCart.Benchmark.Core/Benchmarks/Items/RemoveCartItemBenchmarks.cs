using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeCartItem</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the mutate-existing-cart path — load (real build + recalc), remove the first
/// line item, save (recalc again over the smaller set). Only the I/O leaves are mocked; the totals
/// calculator is real. Idempotent without [IterationSetup] (fresh cart per call — the removal never
/// accumulates). Flat vs Configured surfaces configured-product regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class RemoveCartItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveCartItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateRemoveCartItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveCartItem() => _mediator.Send(_command);
}
