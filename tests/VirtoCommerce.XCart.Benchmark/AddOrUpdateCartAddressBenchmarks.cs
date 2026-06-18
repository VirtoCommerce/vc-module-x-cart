using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addOrUpdateCartAddress</c> GraphQL mutation
/// (<see cref="AddOrUpdateCartAddressCommandHandler.Handle"/>): the upsert-by-key path — load the cart
/// (real build + recalc), find or create an address by Key, apply the address, save (recalc).
/// No Strict validator is wired to address mutations; the handler calls
/// <c>AddOrUpdateCartAddressAsync</c> directly. The address Key matches no pre-existing entry so the
/// new-address (insert) path is measured.
///
/// NOTE: differs from <see cref="AddCartAddressBenchmarks"/> which uses
/// <c>AddOrUpdateCartAddressByTypeAsync</c> (match by AddressType, not Key) — a distinct code path.
///
/// Idempotent without [IterationSetup]: fresh cart per call (no pre-existing address). Two axes.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public class AddOrUpdateCartAddressBenchmarks
{
    private AddOrUpdateCartAddressCommandHandler _handler = null!;
    private readonly AddOrUpdateCartAddressCommand _command =
        CheckoutBenchmarkFixtures.CreateAddOrUpdateCartAddressCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateAddOrUpdateCartAddressHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> AddOrUpdateCartAddress() =>
        _handler.Handle(_command, CancellationToken.None);
}
