using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeWishlist</c> GraphQL mutation
/// (<see cref="ChangeWishlistCommandHandler.Handle"/>). Benchmarks the rename path: load existing
/// wishlist via <c>GetCartByIdAsync</c> (null <c>WishlistUserContext.Cart</c> → real load),
/// update name, skip scope (no scope → all if-branches in <c>UpdateScopeAsync</c> are skipped),
/// then save.
///
/// Complements <see cref="RenameWishlistBenchmarksBase"/>: <c>changeWishlist</c> is the newer
/// mutation that also handles scope/sharing; the rename-only branch is the common case.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public abstract class ChangeWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateChangeWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeWishlist() => _mediator.Send(_command);
}
