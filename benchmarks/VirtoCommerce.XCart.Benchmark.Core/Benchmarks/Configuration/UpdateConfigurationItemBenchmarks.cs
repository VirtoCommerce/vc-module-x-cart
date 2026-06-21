using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>updateConfigurationItem</c> GraphQL mutation
/// (<see cref="UpdateConfigurationItemCommandHandler.Handle"/>): the mutate-existing-cart path —
/// load (real <c>CartAggregateRepository</c> build + recalc), update an existing Variation
/// configuration item on the first configured line item, then save (recalc again). Only the I/O
/// leaves are mocked; the totals calculator and section-matching logic run for real.
///
/// <b>Configured shape only</b>: a flat cart has no <c>ConfigurationItems</c>, so the handler
/// short-circuits at <c>GetConfiguredLineItem</c> without reaching the update logic. The flat
/// shape is excluded intentionally.
///
/// Idempotent without [IterationSetup]: the never-cache + GetAsync-fresh-per-call pattern means
/// each invocation loads a cart where <c>ci-0-0</c> is back to its original state, so the
/// update is always applied to the same baseline.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Configuration)]
public abstract class UpdateConfigurationItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private UpdateConfigurationItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = ConfigurationBenchmarkFixtures.CreateUpdateConfigurationItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> UpdateConfigurationItem() => _mediator.Send(_command);
}
