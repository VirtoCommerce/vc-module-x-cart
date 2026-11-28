using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetCardPaymentMethodsQueryBuilder(IAuthorizationService authorizationService)
    : QueryBuilder<GetCardPaymentMethodsQuery, IList<CardPaymentMethod>, NonNullGraphType<ListGraphType<NonNullGraphType<CardPaymentMethodType>>>>(authorizationService)
{
    protected override string Name => "cardPaymentMethods";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetCardPaymentMethodsQuery request)
    {
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        request.UserId = context.GetCurrentUserId();
        await base.BeforeMediatorSend(context, request);
    }
}
