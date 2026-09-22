using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartConfigurationItemSelected</c> GraphQL
/// mutation (<see cref="ChangeCartConfigurationItemSelectedCommandHandler.Handle"/>): the
/// mutate-existing-cart path — load (real <c>CartAggregateRepository</c> build + recalc),
/// toggle the <c>SelectedForCheckout</c> flag on the first Variation configuration item of the
/// first configured line item, then save (recalc again with the updated price contribution).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Configuration)]
public abstract class ChangeCartConfigurationItemSelectedBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCartConfigurationItemSelectedCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = ConfigurationBenchmarkFixtures.CreateChangeCartConfigurationItemSelectedCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeCartConfigurationItemSelected() => _mediator.Send(_command);
}
