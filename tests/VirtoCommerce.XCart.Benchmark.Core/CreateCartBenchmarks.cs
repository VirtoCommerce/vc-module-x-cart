using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createCart</c> GraphQL mutation
/// (<see cref="CreateCartCommandHandler.Handle"/>): the create-new-cart path (no CartId).
/// Measured compute = search for an existing cart (returns empty → no existing cart),
/// call <c>CreateNewCartAggregateAsync</c> (builds a fresh empty aggregate via
/// <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository.GetCartForShoppingCartAsync"/>
/// which runs the initial recalc over an empty cart), set <c>OrganizationId</c>,
/// then save (recalc again). Only I/O leaves are mocked; the totals calculator is real.
///
/// <b>Shape: Flat only</b>. The newly-created cart is always empty — <c>createCart</c> does not
/// add any items. Configured items are added separately via <c>addCartItems</c>. Parameterising
/// over Configured would measure the same empty-cart path under a different GlobalSetup label,
/// adding noise without signal. LineItemCount is retained as a param axis for harness-cost
/// consistency but has no effect on the measured Handle body (the new cart starts empty).
///
/// <b>Idempotent without [IterationSetup]</b>: each Handle creates a fresh empty cart (the search
/// returns empty on every call) so invocations don't accumulate state.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public class CreateCartBenchmarks
{
    private CreateCartCommandHandler _handler = null!;
    private readonly CreateCartCommand _command = CartStateBenchmarkFixtures.CreateCreateCartCommand();

    // LineItemCount is kept for param-axis consistency; it does not affect the measured cart
    // (the new cart is always empty). Only shape=Flat applies — new cart has no items.
    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartStateBenchmarkFixtures.CreateCreateCartHandler();

    [Benchmark]
    public Task<CartAggregate> CreateCart() => _handler.Handle(_command, CancellationToken.None);
}
