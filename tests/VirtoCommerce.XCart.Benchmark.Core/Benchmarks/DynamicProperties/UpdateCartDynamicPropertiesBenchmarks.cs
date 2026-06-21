using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>updateCartDynamicProperties</c> GraphQL mutation
/// (<see cref="UpdateCartDynamicPropertiesCommandHandler.Handle"/>): the mutate-existing-cart path
/// — load (real build + recalc), delegate to <see cref="CartAggregate.UpdateCartDynamicProperties"/>
/// (which calls <see cref="VirtoCommerce.Platform.Core.DynamicProperties.IDynamicPropertyUpdaterService"/>
/// — a loose mock, no-op), save (recalc again).
///
/// The <see cref="VirtoCommerce.Xapi.Core.Models.DynamicPropertyValue"/> list carries one entry so
/// the aggregate's delegate is invoked. The updater is zero overhead; the dominant cost is the
/// load+recalc envelope common to all mutation handlers. Idempotent without [IterationSetup].
/// Flat vs configured and cart count surfaces recalc regressions independently of the updater.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.DynamicProperties)]
public abstract class UpdateCartDynamicPropertiesBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private UpdateCartDynamicPropertiesCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = GiftsSavedDynamicBenchmarkFixtures.CreateUpdateCartDynamicPropertiesCommand();
    }

    [Benchmark]
    public Task<CartAggregate> UpdateCartDynamicProperties() => _mediator.Send(_command);
}
