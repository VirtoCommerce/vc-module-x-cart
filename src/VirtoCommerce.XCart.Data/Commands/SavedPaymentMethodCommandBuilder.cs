using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

/// <summary>Base for the saved-card mutations: requires a signed-in user and acts for that user only.</summary>
[SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters", Justification = "Mirrors the xAPI CommandBuilder base.")]
public abstract class SavedPaymentMethodCommandBuilder<TCommand, TInputGraphType>(IAuthorizationService authorizationService)
    : CommandBuilder<TCommand, StoredPaymentMethod, TInputGraphType, SavedPaymentMethodType>(authorizationService)
    where TCommand : MediatR.IRequest<StoredPaymentMethod>, ISavedPaymentMethodRequest
    where TInputGraphType : GraphQL.Types.IInputObjectGraphType
{
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TCommand request)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        request.UserId = context.GetCurrentUserId();
        await base.BeforeMediatorSend(context, request);
    }
}
