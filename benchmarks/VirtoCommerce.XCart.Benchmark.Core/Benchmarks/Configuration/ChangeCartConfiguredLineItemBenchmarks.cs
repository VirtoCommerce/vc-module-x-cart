using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartConfiguredLineItem</c> GraphQL mutation
/// (<see cref="ChangeCartConfiguredLineItemCommandHandler.Handle"/>): the mutate-existing-cart
/// path — load (real <c>CartAggregateRepository</c> build + recalc), send a
/// <c>CreateConfiguredLineItemCommand</c> via IMediator (mocked to return a fresh configured
/// item), replace the configuration of the first configured line item, update its price, then
/// save (recalc again). Only the I/O leaves are mocked; the totals calculator and the
/// section-matching / <c>SelectedForCheckout</c> preservation logic run for real.
///
/// <b>Configured shape only</b>: a flat cart has no configured line items; the handler returns
/// early after <c>GetConfiguredLineItem</c> returns null. The flat shape is excluded intentionally.
///
/// Idempotent without [IterationSetup]: the never-cache + GetAsync-fresh-per-call pattern
/// reloads a fresh cart before each invocation. The mediator mock always returns a new item
/// instance, so the line item replacement is always applied to the same baseline.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Configuration)]
public abstract class ChangeCartConfiguredLineItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCartConfiguredLineItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = ConfigurationBenchmarkFixtures.CreateChangeCartConfiguredLineItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeCartConfiguredLineItem() => _mediator.Send(_command);
}
