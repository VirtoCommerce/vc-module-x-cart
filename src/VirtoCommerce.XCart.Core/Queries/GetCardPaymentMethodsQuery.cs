using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetCardPaymentMethodsQuery : Query<IList<CardPaymentMethod>>, ISavedPaymentMethodRequest
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
