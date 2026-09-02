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
/// configuration item from the first configured line item, then save (recalc again).
///
/// Each invocation reloads the full three-item config set, so <c>ci-0-0</c> is always present to
/// remove.
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
