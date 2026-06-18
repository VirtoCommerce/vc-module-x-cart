using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemPrice</c> GraphQL mutation
/// (<see cref="ChangeCartItemPriceCommandHandler.Handle"/>): the mutate-existing-cart path — load
/// (real build + recalc), apply a manual price to the first line item, save (recalc again). Only the
/// I/O leaves are mocked; the totals calculator is real. Idempotent without [IterationSetup] (fresh
/// cart per call via the never-cache harness — see <see cref="CartBenchmarkFixtures.CreateMutationHarness"/>).
/// Flat vs Configured surfaces configured-product regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public class ChangeCartItemPriceBenchmarks
{
    private ChangeCartItemPriceCommandHandler _handler = null!;
    private readonly ChangeCartItemPriceCommand _command = CartBenchmarkFixtures.CreateChangeCartItemPriceCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartBenchmarkFixtures.CreateChangeCartItemPriceHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemPrice() => _handler.Handle(_command, CancellationToken.None);
}
