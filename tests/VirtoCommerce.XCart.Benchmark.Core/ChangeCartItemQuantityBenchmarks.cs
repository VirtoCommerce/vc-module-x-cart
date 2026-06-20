using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemQuantity</c> GraphQL mutation: the
/// mutate-existing-cart path, resolved through <see cref="IMediator"/> so a consuming module's handler
/// override is what runs. The measured compute = load the cart (real repository build + recalc), look
/// up the product, apply the quantity change, then save (recalc again); only I/O leaves are mocked.
///
/// Idempotent without [IterationSetup]: the cart service mock returns a fresh cart per call and the
/// never-cache forces a real load every invocation, so a mutation never accumulates. Two axes: shape
/// (Flat vs Configured) and cart size (100 surfaces super-linear growth).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class ChangeCartItemQuantityBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCartItemQuantityCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateChangeCartItemQuantityCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemQuantity() => _mediator.Send(_command);
}
