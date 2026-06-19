using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartConfigurationItemSelected</c> GraphQL
/// mutation (<see cref="ChangeCartConfigurationItemSelectedCommandHandler.Handle"/>): the
/// mutate-existing-cart path — load (real <c>CartAggregateRepository</c> build + recalc),
/// toggle the <c>SelectedForCheckout</c> flag on the first Variation configuration item of the
/// first configured line item, then save (recalc again with the updated price contribution).
/// Only the I/O leaves are mocked; the totals calculator and the item-lookup / price-update
/// logic run for real.
///
/// <b>Configured shape only</b>: a flat cart has no <c>ConfigurationItems</c>; the handler
/// short-circuits at <c>GetConfiguredLineItem</c> without exercising the selection toggle. The
/// flat shape is excluded intentionally.
///
/// Idempotent without [IterationSetup]: each invocation loads a fresh cart (never-cache +
/// GetAsync mock), so <c>ci-0-0.SelectedForCheckout</c> is always at its initial value (false,
/// the zero-value default of the fixture). The command sets it to true, producing a real price
/// recalculation on every call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Configuration)]
public class ChangeCartConfigurationItemSelectedBenchmarks
{
    private ChangeCartConfigurationItemSelectedCommandHandler _handler = null!;
    private readonly ChangeCartConfigurationItemSelectedCommand _command = ConfigurationBenchmarkFixtures.CreateChangeCartConfigurationItemSelectedCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = ConfigurationBenchmarkFixtures.CreateChangeCartConfigurationItemSelectedHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> ChangeCartConfigurationItemSelected() => _handler.Handle(_command, CancellationToken.None);
}
