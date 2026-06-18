using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeWishlist</c> GraphQL mutation
/// (<see cref="RemoveWishlistCommandHandler.Handle"/>). Measured compute =
/// <c>CartAggregateRepository.RemoveCartAsync</c> → <c>IShoppingCartService.DeleteAsync</c> (no-op
/// mock) + cache expiry token invalidation. Returns null (no aggregate).
///
/// No item-count axis: the handler never loads the cart or recalculates — it issues one delete
/// call regardless of wishlist size. A single invocation measures the delete overhead cleanly.
/// </summary>
[MemoryDiagnoser]
public class RemoveWishlistBenchmarks
{
    private RemoveWishlistCommandHandler _handler = null!;
    private readonly RemoveWishlistCommand _command = WishlistBenchmarkFixtures.CreateRemoveWishlistCommand();

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateRemoveWishlistHandler();

    [Benchmark]
    public Task<CartAggregate> RemoveWishlist() => _handler.Handle(_command, CancellationToken.None);
}
