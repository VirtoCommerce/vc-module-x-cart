using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemComment</c> GraphQL mutation
/// (<see cref="ChangeCartItemCommentCommandHandler.Handle"/>): the mutate-existing-cart path — load
/// (real build + recalc), product-existence check, set the line item comment, save (recalc again).
/// Only the I/O leaves are mocked; the totals calculator is real. Idempotent without [IterationSetup]
/// (fresh cart per call — see <see cref="CartBenchmarkFixtures.CreateMutationHarness"/>). Flat vs
/// Configured surfaces configured-product regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
public class ChangeCartItemCommentBenchmarks
{
    private ChangeCartItemCommentCommandHandler _handler = null!;
    private readonly ChangeCartItemCommentCommand _command = CartBenchmarkFixtures.CreateChangeCartItemCommentCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartBenchmarkFixtures.CreateChangeCartItemCommentHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemComment() => _handler.Handle(_command, CancellationToken.None);
}
