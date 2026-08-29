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
/// <c>CreateConfiguredLineItemCommand</c> through the real mediator (its handler builds a fresh
/// configured item over the mocked product loader), replace the configuration of the first
/// configured line item, update its price, then save (recalc again).
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
