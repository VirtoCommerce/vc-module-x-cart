using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetSavedPaymentMethodsQueryBuilder(IAuthorizationService authorizationService)
    : QueryBuilder<GetSavedPaymentMethodsQuery, IList<StoredPaymentMethod>, NonNullGraphType<ListGraphType<NonNullGraphType<SavedPaymentMethodType>>>>(authorizationService)
{
    protected override string Name => "savedPaymentMethods";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetSavedPaymentMethodsQuery request)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        request.UserId = context.GetCurrentUserId();
        await base.BeforeMediatorSend(context, request);
    }
}
