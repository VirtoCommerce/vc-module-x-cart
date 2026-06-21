using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>initializeCartPayment</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the end-to-end cart-pay path — load the cart by Id (GetCartByIdAsync, not
/// GetOrCreateCartFromCommandAsync), locate the pre-seeded payment, resolve the available payment
/// method, assert <c>AllowCartPayment = true</c>, call <c>ProcessPaymentAsync</c> (mocked → immediate
/// success result), and build the result.
///
/// This handler is NOT a <c>CartCommandHandler{TCommand}</c> and returns
/// <see cref="InitializeCartPaymentResult"/> rather than <see cref="VirtoCommerce.XCart.Core.CartAggregate"/>.
/// The cart fixture pre-seeds one payment (Id = <c>payment-0</c>, GatewayCode = <c>bench-pay</c>) via the
/// <c>customizeCart</c> hook (<see cref="CheckoutBenchmarkFixtures.SeedPayment"/>) so the handler finds
/// its target. The mocked payment method has <c>AllowCartPayment = true</c> and returns a success result
/// from <c>ProcessPaymentAsync</c>.
///
/// Idempotent without [IterationSetup]: GetAsync mock returns a fresh cart per call and the
/// never-cache forces a real load every invocation. Two axes: shape and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public abstract class InitializeCartPaymentBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private InitializeCartPaymentCommand _command = null!;

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
            customizeCart: CheckoutBenchmarkFixtures.SeedPayment,
            customizeServices: s => s.AddSingleton<ICartAvailMethodsService>(CheckoutBenchmarkFixtures.PaymentAvailMethodsService()))
            .GetRequiredService<IMediator>();
        _command = CheckoutBenchmarkFixtures.CreateInitializeCartPaymentCommand();
    }

    [Benchmark]
    public Task<InitializeCartPaymentResult> InitializeCartPayment() => _mediator.Send(_command);
}
