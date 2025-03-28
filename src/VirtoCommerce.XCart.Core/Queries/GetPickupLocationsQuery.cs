using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetPickupLocationsQuery : SearchQuery<PickupLocationsResponse>
{
    public string StoreId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        return base.GetArguments().Union([
            Argument<StringGraphType>(nameof(StoreId)),
        ]);
    }

    public override void Map(IResolveFieldContext context)
    {
        StoreId = context.GetArgument<string>(nameof(StoreId));
        base.Map(context);
    }
}
