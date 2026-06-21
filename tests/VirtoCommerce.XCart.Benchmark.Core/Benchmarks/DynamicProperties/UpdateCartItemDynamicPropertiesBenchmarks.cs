using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>updateCartItemDynamicProperties</c> GraphQL mutation
/// (<see cref="UpdateCartItemDynamicPropertiesCommandHandler.Handle"/>): the mutate-existing-cart
/// path — load (real build + recalc), look up <c>li-0</c> in <c>Cart.Items</c>, delegate to
/// <see cref="CartAggregate.UpdateCartItemDynamicProperties(string, System.Collections.Generic.IList{VirtoCommerce.Xapi.Core.Models.DynamicPropertyValue})"/>
/// (no-op loose mock updater), save (recalc again).
///
/// The command targets the first line item (<c>li-0</c>) which is present in every fixture cart
/// regardless of size, ensuring the updater delegate is always reached (not the null-guard path).
/// The updater is zero overhead; dominant cost is load+recalc. Idempotent without [IterationSetup].
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.DynamicProperties)]
public abstract class UpdateCartItemDynamicPropertiesBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private UpdateCartItemDynamicPropertiesCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = GiftsSavedDynamicBenchmarkFixtures.CreateUpdateCartItemDynamicPropertiesCommand();
    }

    [Benchmark]
    public Task<CartAggregate> UpdateCartItemDynamicProperties() => _mediator.Send(_command);
}
