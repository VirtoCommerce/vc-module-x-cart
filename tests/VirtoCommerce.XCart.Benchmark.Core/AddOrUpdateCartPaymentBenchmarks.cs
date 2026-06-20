using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addOrUpdateCartPayment</c> GraphQL mutation, resolved
/// through <see cref="IMediator"/>: the add-new-payment path — load the cart (real build + recalc),
/// run payment validation against the mocked available methods, add the payment, save (recalc). The
/// <c>CartPaymentValidator</c> runs in Strict mode (ThrowOnFailures); the fixture supplies a gateway
/// code that matches the mocked method so the validator passes.
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart (Payments = []) per
/// call and the never-cache forces a real load every invocation. Two axes: shape and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class AddOrUpdateCartPaymentBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddOrUpdateCartPaymentCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(
            LineItemCount,
            Shape,
            customizeServices: s => s.AddSingleton<ICartAvailMethodsService>(CheckoutBenchmarkFixtures.PaymentAvailMethodsService()))
            .GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateAddOrUpdateCartPaymentCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddOrUpdateCartPayment() => _mediator.Send(_command);
}
