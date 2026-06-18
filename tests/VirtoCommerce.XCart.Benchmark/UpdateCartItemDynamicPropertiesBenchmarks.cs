using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

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
public class UpdateCartItemDynamicPropertiesBenchmarks
{
    private UpdateCartItemDynamicPropertiesCommandHandler _handler = null!;
    private readonly UpdateCartItemDynamicPropertiesCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateUpdateCartItemDynamicPropertiesCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateUpdateCartItemDynamicPropertiesHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> UpdateCartItemDynamicProperties() => _handler.Handle(_command, CancellationToken.None);
}
