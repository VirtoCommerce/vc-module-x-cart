using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetSavedPaymentMethodsQuery : Query<IList<StoredPaymentMethod>>, ISavedPaymentMethodRequest
{
    public string StoreId { get; set; }
    public string UserId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId));
    }

    public override void Map(IResolveFieldContext context)
    {
        StoreId = context.GetArgument<string>(nameof(StoreId));
    }
}
