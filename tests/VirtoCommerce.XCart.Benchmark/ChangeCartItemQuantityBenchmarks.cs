using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemQuantity</c> GraphQL mutation
/// (<see cref="ChangeCartItemQuantityCommandHandler.Handle"/>): the mutate-existing-cart path. The
/// measured compute = load the cart (real <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/>
/// build + recalc), look up the product, apply the quantity change, then save (recalc again);
/// only I/O leaves are mocked (DB read/write, never-cache).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call and the
/// never-cache forces a real load every invocation, so a mutation never accumulates. That keeps
/// InvocationCount unforced and Mean precise.
///
/// Two axes: <b>shape</b> (Flat vs Configured — the configured cart carries a configuration-item set
/// the load-path recalc and the save-path recalc both walk, so a configured-product regression shows
/// up here) and the cart-size count (100 surfaces super-linear growth).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public class ChangeCartItemQuantityBenchmarks
{
    private ChangeCartItemQuantityCommandHandler _handler = null!;
    private readonly ChangeCartItemQuantityCommand _command = CartBenchmarkFixtures.CreateChangeCartItemQuantityCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartBenchmarkFixtures.CreateChangeCartItemQuantityHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemQuantity() => _handler.Handle(_command, CancellationToken.None);
}
