using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Queries;

/// <summary>
/// The store's active payment methods that can save cards, each initialized for a card saved without an order
/// (<see cref="ProcessPaymentRequest.StoreForFutureUse"/>), so the storefront can load the provider's card form.
/// </summary>
public class GetCardPaymentMethodsQueryHandler(IPaymentMethodsSearchService paymentMethodsSearchService, ILogger<GetCardPaymentMethodsQueryHandler> logger)
    : IQueryHandler<GetCardPaymentMethodsQuery, IList<CardPaymentMethod>>
{
    private const int MaxPaymentMethods = 50;

    public virtual async Task<IList<CardPaymentMethod>> Handle(GetCardPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var criteria = AbstractTypeFactory<PaymentMethodsSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.IsActive = true;
        criteria.Take = MaxPaymentMethods;

        var paymentMethods = (await paymentMethodsSearchService.SearchAsync(criteria)).Results
            .Where(x => x is ISupportStoredCredentialFlow)
            .OrderBy(x => x.Priority)
            .ToList();

        var result = new List<CardPaymentMethod>();
        foreach (var paymentMethod in paymentMethods)
        {
            var cardPaymentMethod = await InitializeAsync(paymentMethod, request, cancellationToken);
            if (cardPaymentMethod != null)
            {
                result.Add(cardPaymentMethod);
            }
        }

        return result;
    }

    protected virtual async Task<CardPaymentMethod> InitializeAsync(PaymentMethod paymentMethod, GetCardPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var initialization = await paymentMethod.ProcessPaymentAsync(new ProcessPaymentRequest
            {
                StoreId = request.StoreId,
                StoreForFutureUse = true,
            }, cancellationToken);

            if (initialization?.IsSuccess == false)
            {
                return null;
            }

            var result = AbstractTypeFactory<CardPaymentMethod>.TryCreateInstance();
            result.PaymentMethodCode = paymentMethod.Code;
            result.Name = paymentMethod.Name;
            result.LogoUrl = paymentMethod.LogoUrl;
            result.PublicParameters = initialization?.PublicParameters ?? new Dictionary<string, string>();
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Payment method {PaymentMethodCode} can't prepare a card form without an order", paymentMethod.Code);
            return null;
        }
    }
}
