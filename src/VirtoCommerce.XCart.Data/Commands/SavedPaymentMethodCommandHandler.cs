using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Data.Commands;

/// <summary>
/// The signed-in customer's saved-card mutations. The card rules (ownership, default card, cards in use) live in the
/// Payment module's <see cref="IStoredPaymentMethodManager"/>; this handler only maps its results to GraphQL errors.
/// </summary>
public class SavedPaymentMethodCommandHandler(IStoredPaymentMethodManager storedPaymentMethodManager)
    : IRequestHandler<AddSavedPaymentMethodCommand, StoredPaymentMethod>,
        IRequestHandler<RemoveSavedPaymentMethodCommand, StoredPaymentMethod>,
        IRequestHandler<SetDefaultSavedPaymentMethodCommand, StoredPaymentMethod>
{
    public const string AuthenticationRequiredCode = "AUTHENTICATION_REQUIRED";
    public const string SavedPaymentMethodErrorCode = "SAVED_PAYMENT_METHOD_ERROR";

    public virtual async Task<StoredPaymentMethod> Handle(AddSavedPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var parameters = new NameValueCollection();
        foreach (var parameter in request.Parameters ?? [])
        {
            parameters[parameter.Key] = parameter.Value;
        }

        var addRequest = AbstractTypeFactory<AddStoredPaymentMethodRequest>.TryCreateInstance();
        addRequest.StoreId = request.StoreId;
        addRequest.CustomerId = request.UserId;
        addRequest.PaymentMethodCode = request.PaymentMethodCode;
        addRequest.Parameters = parameters;
        addRequest.SetDefault = request.SetDefault == true;

        return ToResult(await storedPaymentMethodManager.AddAsync(addRequest));
    }

    public virtual async Task<StoredPaymentMethod> Handle(RemoveSavedPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        return ToResult(await storedPaymentMethodManager.RemoveAsync(request.UserId, request.Id));
    }

    public virtual async Task<StoredPaymentMethod> Handle(SetDefaultSavedPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        return ToResult(await storedPaymentMethodManager.SetDefaultAsync(request.UserId, request.Id));
    }

    protected virtual StoredPaymentMethod ToResult(StoredPaymentMethodOperationResult result)
    {
        if (result.IsSuccess)
        {
            return result.StoredPaymentMethod;
        }

        var code = result.Outcome == ChargeOutcome.RequiresAction ? AuthenticationRequiredCode : SavedPaymentMethodErrorCode;
        throw new ExecutionError(result.ErrorMessage ?? "The card could not be saved.") { Code = code };
    }
}
