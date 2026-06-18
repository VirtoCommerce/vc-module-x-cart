using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addConfigurationItem</c> GraphQL mutation
/// (<see cref="AddConfigurationItemCommandHandler.Handle"/>): the mutate-existing-cart path —
/// load (real <c>CartAggregateRepository</c> build + recalc), add a new Variation section to
/// the first configured line item, then save (recalc again). Only the I/O leaves are mocked;
/// the totals calculator and section-matching logic run for real.
///
/// <b>Configured shape only</b>: a flat cart has no <c>ConfigurationItems</c>, so the handler
/// would short-circuit at <c>GetConfiguredLineItem</c> (returns null) without reaching the add
/// logic. <c>[Params(CartShape.Configured)]</c> is used as a self-documenting annotation of that
/// constraint; omitting the flat shape is intentional, not an oversight.
///
/// Idempotent without [IterationSetup]: the never-cache mock forces a real cart reload per call,
/// and the GetAsync mock returns a fresh cart each time so the added item never accumulates.
/// </summary>
[MemoryDiagnoser]
public class AddConfigurationItemBenchmarks
{
    private AddConfigurationItemCommandHandler _handler = null!;
    private readonly AddConfigurationItemCommand _command = ConfigurationBenchmarkFixtures.CreateAddConfigurationItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = ConfigurationBenchmarkFixtures.CreateAddConfigurationItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> AddConfigurationItem() => _handler.Handle(_command, CancellationToken.None);
}
