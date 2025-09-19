using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCatalog.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class CartPickupLocationsQuery : SearchQuery<ProductPickupLocationSearchResult>
{
    public string CartId { get; set; }

    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(CartId), description: "Cart Id");
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(CultureName), description: "Culture name (\"en-US\")");
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        CartId = context.GetArgument<string>(nameof(CartId));
        StoreId = context.GetArgument<string>(nameof(StoreId));
        CultureName = context.GetArgument<string>(nameof(CultureName));
    }
}
