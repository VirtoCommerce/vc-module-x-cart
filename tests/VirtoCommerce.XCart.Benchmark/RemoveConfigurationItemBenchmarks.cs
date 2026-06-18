using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

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
public class RemoveConfigurationItemBenchmarks
{
    private RemoveConfigurationItemCommandHandler _handler = null!;
    private readonly RemoveConfigurationItemCommand _command = ConfigurationBenchmarkFixtures.CreateRemoveConfigurationItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = ConfigurationBenchmarkFixtures.CreateRemoveConfigurationItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> RemoveConfigurationItem() => _handler.Handle(_command, CancellationToken.None);
}
