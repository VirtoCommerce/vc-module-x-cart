using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class InitializeCartPaymentCommandHandler(
    ICartAggregateRepository cartAggregateRepository,
    IPaymentMethodsSearchService paymentMethodsSearchService) : IRequestHandler<InitializeCartPaymentCommand, InitializeCartPaymentResult>
{
    public async Task<InitializeCartPaymentResult> Handle(InitializeCartPaymentCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);

        var payment = cart.Cart.Payments.FirstOrDefault();

        if (payment == null)
        {
            throw new InvalidOperationException($"Payment not found in cart '{request.CartId}'");
        }

        //var validationResult = AbstractTypeFactory<PaymentRequestValidator>.TryCreateInstance().Validate(paymentInfo);
        //if (!validationResult.IsValid)
        //{
        //    return ErrorResult<InitializePaymentResult>(validationResult.Errors.FirstOrDefault()?.ErrorMessage);
        //}

        var paymentMethods = await paymentMethodsSearchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = request.StoreId });

        var paymentMethod = paymentMethods.Results.FirstOrDefault(pm => pm.Code.EqualsIgnoreCase(payment.PaymentGatewayCode));

        if (paymentMethod == null)
        {
            throw new InvalidOperationException($"Payment method with code '{payment.PaymentGatewayCode}' not found.");
        }

        var processPaymentRequest = new ProcessPaymentRequest
        {
            PaymentId = payment.Id,
            StoreId = request.StoreId,
            Parameters = new()
            {
                {"Amount", cart.Cart.Total.ToString(CultureInfo.InvariantCulture)},
                {"Currency", cart.Cart.Currency},
                {"CartId", request.CartId}
            },
        };

        var processPaymentResult = paymentMethod.ProcessPayment(processPaymentRequest);

        var result = new InitializeCartPaymentResult
        {
            StoreId = processPaymentRequest.StoreId,
            PaymentId = processPaymentRequest.PaymentId,
            IsSuccess = processPaymentResult.IsSuccess,
            ErrorMessage = processPaymentResult.ErrorMessage,
            ActionHtmlForm = processPaymentResult.HtmlForm,
            ActionRedirectUrl = processPaymentResult.RedirectUrl,
            PublicParameters = processPaymentResult.PublicParameters,
            PaymentMethodCode = paymentMethod.Code,
            PaymentActionType = paymentMethod.PaymentMethodType.ToString()
        };

        return result;
    }
}
