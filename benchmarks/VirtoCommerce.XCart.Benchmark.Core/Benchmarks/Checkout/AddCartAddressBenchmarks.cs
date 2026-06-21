using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCartAddress</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the upsert-by-address-type path — load the cart (real build + recalc),
/// find or create an address by AddressType (Shipping), apply the address, save (recalc).
///
/// NOTE: differs from <see cref="AddOrUpdateCartAddressBenchmarksBase"/> which uses
/// <c>AddOrUpdateCartAddressAsync</c> (match by Key) — <c>AddCartAddressCommandHandler</c> calls
/// <c>AddOrUpdateCartAddressByTypeAsync</c> instead, exercising a distinct lookup path that replaces
/// any existing address of the same type rather than matching on the Key field.
///
/// Idempotent without [IterationSetup]: fresh cart per call (no pre-existing address). Two axes.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class AddCartAddressBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddCartAddressCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateAddCartAddressCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddCartAddress() => _mediator.Send(_command);
}
