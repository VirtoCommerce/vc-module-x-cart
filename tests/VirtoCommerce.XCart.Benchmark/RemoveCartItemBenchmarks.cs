using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeCartItem</c> GraphQL mutation
/// (<see cref="RemoveCartItemCommandHandler.Handle"/>): the mutate-existing-cart path — load (real
/// build + recalc), remove the first line item, save (recalc again over the smaller set). Only the
/// I/O leaves are mocked; the totals calculator is real. Idempotent without [IterationSetup] (fresh
/// cart per call — the removal never accumulates). Flat vs Configured surfaces configured-product
/// regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public class RemoveCartItemBenchmarks
{
    private RemoveCartItemCommandHandler _handler = null!;
    private readonly RemoveCartItemCommand _command = CartBenchmarkFixtures.CreateRemoveCartItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartBenchmarkFixtures.CreateRemoveCartItemHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> RemoveCartItem() => _handler.Handle(_command, CancellationToken.None);
}
