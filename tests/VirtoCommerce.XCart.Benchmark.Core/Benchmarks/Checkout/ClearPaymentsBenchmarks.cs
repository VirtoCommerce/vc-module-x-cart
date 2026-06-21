using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>clearPayments</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: load the cart (real build + recalc), call <c>ClearPaymentsAsync</c>
/// (sets Payments = []), save (recalc).
///
/// Same semantics as <see cref="ClearShipmentsBenchmarksBase"/>: the load + empty-collection-assign +
/// save + recalc cycle is measured on a cart with empty Payments. The full item graph walk (load
/// recalc + save recalc) is still the dominant cost. Two axes: shape and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class ClearPaymentsBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ClearPaymentsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateClearPaymentsCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ClearPayments() => _mediator.Send(_command);
}
