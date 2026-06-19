using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeShipment</c> GraphQL mutation
/// (<see cref="RemoveShipmentCommandHandler.Handle"/>): the remove-existing-shipment path — load the
/// cart (real build + recalc), remove the pre-seeded shipment by Id, save (recalc).
///
/// FLAG — shared-fixture limitation: <c>CartBenchmarkFixtures.CreateCart</c> seeds <c>Shipments = []</c>.
/// <c>RemoveShipmentCommandHandler</c> is a no-op when the target shipment is not found (Remove on an
/// empty list never throws). To measure the actual remove path this benchmark uses a dedicated
/// repository from <see cref="CheckoutBenchmarkFixtures.CreateRemoveShipmentHandler"/> whose GetAsync
/// returns a cart pre-seeded with one shipment (<see cref="CheckoutBenchmarkFixtures.SeededShipmentId"/>).
///
/// Idempotent without [IterationSetup]: GetAsync mock returns a fresh cart per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public class RemoveShipmentBenchmarks
{
    private RemoveShipmentCommandHandler _handler = null!;
    private readonly RemoveShipmentCommand _command =
        CheckoutBenchmarkFixtures.CreateRemoveShipmentCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateRemoveShipmentHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> RemoveShipment() =>
        _handler.Handle(_command, CancellationToken.None);
}
