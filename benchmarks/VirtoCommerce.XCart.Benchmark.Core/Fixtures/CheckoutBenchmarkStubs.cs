using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Benchmark;

// Minimal concrete stubs of abstract platform types used by the checkout benchmarks. Mirrors the
// pattern of StubShippingMethod / StubPaymentMethod in the test project, but kept local here so the
// benchmark project has no dependency on the test project.

/// <summary>
/// Concrete <see cref="ShippingMethod"/> for benchmark use.
/// <see cref="CalculateRates"/> is never called by <c>AddShipmentAsync</c>
/// (rates come from the mocked <c>ICartAvailMethodsService</c>), so it is
/// left as <see cref="System.NotImplementedException"/>.
/// </summary>
internal sealed class BenchmarkShippingMethod(string code) : ShippingMethod(code)
{
    public override IEnumerable<ShippingRate> CalculateRates(IEvaluationContext context) =>
        throw new System.NotImplementedException("benchmark stub — not called");
}

/// <summary>
/// Concrete <see cref="PaymentMethod"/> for benchmark use.
/// <see cref="ProcessPaymentAsync"/> is called by
/// <c>InitializeCartPaymentCommandHandler.Handle</c> when <c>AllowCartPayment</c> is
/// true, and returns success unless <see cref="SetProcessPaymentResult"/> overrides it.
/// </summary>
internal sealed class BenchmarkPaymentMethod(string code) : PaymentMethod(code)
{
    private ProcessPaymentRequestResult _result = new() { IsSuccess = true };

    public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    public override PaymentMethodGroupType PaymentMethodGroupType =>
        PaymentMethodGroupType.Alternative;

    /// <summary>Must be true for <c>InitializeCartPaymentCommandHandler</c> to proceed past its
    /// guard check.</summary>
    public override bool AllowCartPayment => true;

    public void SetProcessPaymentResult(ProcessPaymentRequestResult result) => _result = result;

    public override Task<ProcessPaymentRequestResult> ProcessPaymentAsync(
        ProcessPaymentRequest paymentRequest,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}
