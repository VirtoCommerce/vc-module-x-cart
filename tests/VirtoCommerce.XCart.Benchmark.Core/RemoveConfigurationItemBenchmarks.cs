using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeConfigurationItem</c> GraphQL mutation
/// (<see cref="RemoveConfigurationItemCommandHandler.Handle"/>): the mutate-existing-cart path —
/// load (real <c>CartAggregateRepository</c> build + recalc), remove the first Variation
/// configuration item from the first configured line item, then save (recalc again). Only the
/// I/O leaves are mocked; the totals calculator runs for real.
///
/// <b>Configured shape only</b>: a flat cart has no <c>ConfigurationItems</c>, so the handler
/// short-circuits at <c>GetConfiguredLineItem</c> without reaching the remove logic. The flat
/// shape is excluded intentionally.
///
/// Idempotent without [IterationSetup]: the never-cache + GetAsync-fresh-per-call pattern
/// reloads a full three-item config set before each invocation, so the remove is always applied
/// to a cart where <c>ci-0-0</c> is present.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Configuration)]
public abstract class RemoveConfigurationItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveConfigurationItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = ConfigurationBenchmarkFixtures.CreateRemoveConfigurationItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveConfigurationItem() => _mediator.Send(_command);
}
