using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addOrUpdateCartAddress</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the upsert-by-key path — load the cart (real build + recalc), find or
/// create an address by Key, apply the address, save (recalc). No Strict validator is wired to address
/// mutations; the handler calls <c>AddOrUpdateCartAddressAsync</c> directly. The address Key matches no
/// pre-existing entry so the new-address (insert) path is measured.
///
/// NOTE: differs from <see cref="AddCartAddressBenchmarksBase"/> which uses
/// <c>AddOrUpdateCartAddressByTypeAsync</c> (match by AddressType, not Key) — a distinct code path.
///
/// Idempotent without [IterationSetup]: fresh cart per call (no pre-existing address). Two axes.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class AddOrUpdateCartAddressBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddOrUpdateCartAddressCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateAddOrUpdateCartAddressCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddOrUpdateCartAddress() => _mediator.Send(_command);
}
