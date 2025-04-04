using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.XCart.Core.Queries;

public class PickupLocationsQuery : SearchQuery<PickupLocationSearchResult>
{
    public string StoreId { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        return base.GetArguments().Concat([
            Argument<StringGraphType>(nameof(StoreId)),
        ]);
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);
        StoreId = context.GetArgument<string>(nameof(StoreId));
    }
}
