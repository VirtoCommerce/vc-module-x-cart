using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>initializeCartPayment</c> GraphQL mutation
/// (<see cref="InitializeCartPaymentCommandHandler.Handle"/>): the end-to-end cart-pay path — load the
/// cart by Id (GetCartByIdAsync, not GetOrCreateCartFromCommandAsync), locate the pre-seeded payment,
/// resolve the available payment method, assert <c>AllowCartPayment = true</c>, call
/// <c>ProcessPaymentAsync</c> (mocked → immediate success result), and build the result.
///
/// This handler is NOT a <c>CartCommandHandler{TCommand}</c> and returns
/// <see cref="InitializeCartPaymentResult"/> rather than <see cref="VirtoCommerce.XCart.Core.CartAggregate"/>.
/// The cart fixture pre-seeds one payment (Id = <c>payment-0</c>, GatewayCode = <c>bench-pay</c>) so
/// the handler finds its target. The mocked payment method has <c>AllowCartPayment = true</c> and
/// returns a success result from <c>ProcessPaymentAsync</c>.
///
/// FLAG — shared-fixture limitation: <c>CartBenchmarkFixtures.CreateCart</c> seeds <c>Payments = []</c>;
/// this benchmark uses <see cref="CheckoutBenchmarkFixtures.CreateInitializeCartPaymentHandler"/> which
/// builds a dedicated repository whose GetAsync returns <see cref="CheckoutBenchmarkFixtures.CreateCartWithPayment"/>.
///
/// Idempotent without [IterationSetup]: GetAsync mock returns a fresh cart per call and the
/// never-cache forces a real load every invocation. Two axes: shape and cart size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Checkout)]
public class InitializeCartPaymentBenchmarks
{
    private InitializeCartPaymentCommandHandler _handler = null!;
    private readonly InitializeCartPaymentCommand _command =
        CheckoutBenchmarkFixtures.CreateInitializeCartPaymentCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() =>
        _handler = CheckoutBenchmarkFixtures.CreateInitializeCartPaymentHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<InitializeCartPaymentResult> InitializeCartPayment() =>
        _handler.Handle(_command, CancellationToken.None);
}
