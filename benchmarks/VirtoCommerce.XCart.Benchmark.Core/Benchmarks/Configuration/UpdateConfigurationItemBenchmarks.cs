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
/// configuration item on the first configured line item, then save (recalc again).
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
