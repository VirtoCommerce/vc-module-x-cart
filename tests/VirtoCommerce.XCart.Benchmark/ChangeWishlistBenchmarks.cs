using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeWishlist</c> GraphQL mutation
/// (<see cref="ChangeWishlistCommandHandler.Handle"/>). Benchmarks the rename path: load existing
/// wishlist via <c>GetCartByIdAsync</c> (null <c>WishlistUserContext.Cart</c> → real load),
/// update name, skip scope (no scope → all if-branches in <c>UpdateScopeAsync</c> are skipped),
/// then save.
///
/// Complements <see cref="RenameWishlistBenchmarks"/>: <c>changeWishlist</c> is the newer
/// mutation that also handles scope/sharing; the rename-only branch is the common case.
/// </summary>
[MemoryDiagnoser]
public class ChangeWishlistBenchmarks
{
    private ChangeWishlistCommandHandler _handler = null!;
    private readonly ChangeWishlistCommand _command = WishlistBenchmarkFixtures.CreateChangeWishlistCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateChangeWishlistHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> ChangeWishlist() => _handler.Handle(_command, CancellationToken.None);
}
