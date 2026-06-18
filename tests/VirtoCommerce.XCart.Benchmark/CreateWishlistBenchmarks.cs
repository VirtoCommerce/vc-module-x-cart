using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createWishlist</c> GraphQL mutation
/// (<see cref="CreateWishlistCommandHandler.Handle"/>). Measured compute = new in-memory
/// <see cref="VirtoCommerce.CartModule.Core.Model.ShoppingCart"/> construction →
/// <c>CartAggregateRepository.GetCartForShoppingCartAsync</c> (currency + store load, real
/// <c>RecalculateAsync</c>) → no-op <c>SaveAsync</c>.
///
/// The search mock returns empty every call so the handler always creates a new cart; no
/// [IterationSetup] needed. Shape is fixed at <see cref="CartShape.Flat"/> (wishlists hold flat
/// product references — no configured-item semantics benchmarked here).
/// </summary>
[MemoryDiagnoser]
public class CreateWishlistBenchmarks
{
    private CreateWishlistCommandHandler _handler = null!;
    private CreateWishlistCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _handler = WishlistBenchmarkFixtures.CreateWishlistHandler();
        _command = WishlistBenchmarkFixtures.CreateWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> CreateWishlist() => _handler.Handle(_command, CancellationToken.None);
}
