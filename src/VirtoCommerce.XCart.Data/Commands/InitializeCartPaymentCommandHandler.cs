using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class InitializeCartPaymentCommandHandler(
    ICartAggregateRepository cartAggregateRepository,
    ICartAvailMethodsService cartAvailMethodsService) : IRequestHandler<InitializeCartPaymentCommand, InitializeCartPaymentResult>
{
    public async Task<InitializeCartPaymentResult> Handle(InitializeCartPaymentCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartAggregateRepository.GetCartByIdAsync(request.CartId);

        if (cart == null)
        {
            throw new InvalidOperationException($"Cart '{request.CartId}' not found ");
        }

        var payment = cart.Cart.Payments.FirstOrDefault(x => x.Id == request.PaymentId);

        if (payment == null)
        {
            throw new InvalidOperationException($"Payment not found in cart '{request.CartId}'");
        }

        var paymentMethods = await cartAvailMethodsService.GetAvailablePaymentMethodsAsync(cart);
        var paymentMethod = paymentMethods.FirstOrDefault(pm => pm.Code.EqualsIgnoreCase(payment.PaymentGatewayCode));

        if (paymentMethod == null)
        {
            throw new InvalidOperationException($"Payment method with code '{payment.PaymentGatewayCode}' not found.");
        }

        if (!paymentMethod.AllowCartPayment)
        {
            throw new InvalidOperationException($"Payment method '{payment.PaymentGatewayCode}' doesn't allowed in cart.");
        }

        var processPaymentRequest = await CreateProcessPaymentRequest(request, cart, payment, cancellationToken);
        var processPaymentResult = paymentMethod.ProcessPayment(processPaymentRequest);
        var result = await CreateInitializeCartPaymentResult(paymentMethod, processPaymentRequest, processPaymentResult, cancellationToken);

        return result;
    }

    protected virtual Task<ProcessPaymentRequest> CreateProcessPaymentRequest(InitializeCartPaymentCommand request, CartAggregate cart, Payment payment, CancellationToken cancellationToken)
    {
        var result = AbstractTypeFactory<ProcessPaymentRequest>.TryCreateInstance();
        result.PaymentId = payment.Id;
        result.StoreId = cart.Store.Id;
        result.Store = cart.Store;
        result.Parameters = new()
        {
            ["CartId"] = request.CartId,
            ["Amount"] = cart.Cart.Total.ToString(CultureInfo.InvariantCulture),
            ["Currency"] = cart.Cart.Currency,
        };
        return Task.FromResult(result);
    }

    protected virtual async Task<InitializeCartPaymentResult> CreateInitializeCartPaymentResult(PaymentMethod paymentMethod, ProcessPaymentRequest processPaymentRequest, ProcessPaymentRequestResult processPaymentResult, CancellationToken cancellationToken)
    {
        var result = AbstractTypeFactory<InitializeCartPaymentResult>.TryCreateInstance();

        result.StoreId = processPaymentRequest.StoreId;
        result.PaymentId = processPaymentRequest.PaymentId;
        result.IsSuccess = processPaymentResult.IsSuccess;
        result.ErrorMessage = processPaymentResult.ErrorMessage;
        result.ActionHtmlForm = processPaymentResult.HtmlForm;
        result.ActionRedirectUrl = processPaymentResult.RedirectUrl;
        result.PublicParameters = processPaymentResult.PublicParameters;
        result.PaymentMethodCode = paymentMethod.Code;
        result.PaymentActionType = paymentMethod.PaymentMethodType.ToString();

        return await Task.FromResult(result);
    }
}
