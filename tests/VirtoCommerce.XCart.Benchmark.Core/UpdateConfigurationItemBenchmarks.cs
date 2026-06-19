using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

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
public class UpdateConfigurationItemBenchmarks
{
    private UpdateConfigurationItemCommandHandler _handler = null!;
    private readonly UpdateConfigurationItemCommand _command = ConfigurationBenchmarkFixtures.CreateUpdateConfigurationItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = ConfigurationBenchmarkFixtures.CreateUpdateConfigurationItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> UpdateConfigurationItem() => _handler.Handle(_command, CancellationToken.None);
}
