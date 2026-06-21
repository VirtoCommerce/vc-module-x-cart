using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeShipment</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the remove-existing-shipment path — load the cart (real build + recalc),
/// remove the pre-seeded shipment by Id, save (recalc).
///
/// FLAG — shared-fixture limitation: <c>CartBenchmarkFixtures.CreateCart</c> seeds <c>Shipments = []</c>.
/// <c>RemoveShipmentCommandHandler</c> is a no-op when the target shipment is not found (Remove on an
/// empty list never throws). To measure the actual remove path this benchmark seeds the cart with one
/// shipment (<see cref="CheckoutBenchmarkFixtures.SeededShipmentId"/>) via the <c>customizeCart</c>
/// hook (<see cref="CheckoutBenchmarkFixtures.SeedShipment"/>).
///
/// Idempotent without [IterationSetup]: GetAsync mock returns a fresh cart per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class RemoveShipmentBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveShipmentCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape, customizeCart: CheckoutBenchmarkFixtures.SeedShipment)
            .GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateRemoveShipmentCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveShipment() => _mediator.Send(_command);
}
