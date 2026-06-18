using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCartAddress</c> GraphQL mutation
/// (<see cref="AddCartAddressCommandHandler.Handle"/>): the upsert-by-address-type path — load the
/// cart (real build + recalc), find or create an address by AddressType (Shipping), apply the
/// address, save (recalc).
///
/// NOTE: differs from <see cref="AddOrUpdateCartAddressBenchmarks"/> which uses
/// <c>AddOrUpdateCartAddressAsync</c> (match by Key) — <c>AddCartAddressCommandHandler</c> calls
/// <c>AddOrUpdateCartAddressByTypeAsync</c> instead, exercising a distinct lookup path that replaces
/// any existing address of the same type rather than matching on the Key field.
///
/// Idempotent without [IterationSetup]: fresh cart per call (no pre-existing address). Two axes.
/// </summary>
[MemoryDiagnoser]
public class AddCartAddressBenchmarks
{
    private AddCartAddressCommandHandler _handler = null!;
    private readonly AddCartAddressCommand _command =
        CheckoutBenchmarkFixtures.CreateAddCartAddressCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateAddCartAddressHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> AddCartAddress() =>
        _handler.Handle(_command, CancellationToken.None);
}
