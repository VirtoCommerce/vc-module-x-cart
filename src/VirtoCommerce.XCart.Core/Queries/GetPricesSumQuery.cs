using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetPricesSumQuery : Query<ExpPricesSum>
{
    public string CartId { get; set; }
    public string StoreId { get; set; }
    public string UserId { get; set; }
    public string OrganizationId { get; set; }
    public string CurrencyCode { get; set; }
    public string CultureName { get; set; }

    public IList<string> LineItemIds { get; set; } = Array.Empty<string>();

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(CartId));
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(CurrencyCode), description: "Currency code (\"USD\")");
        yield return Argument<StringGraphType>(nameof(CultureName), description: "Culture name (\"en-Us\")");
        yield return Argument<StringGraphType>(nameof(UserId), description: "User Id");

        yield return Argument<NonNullGraphType<ListGraphType<StringGraphType>>>(nameof(LineItemIds), description: "Line item Id");
    }

    public override void Map(IResolveFieldContext context)
    {
        CartId = context.GetArgument<string>(nameof(CartId));
        StoreId = context.GetArgument<string>(nameof(StoreId));
        UserId = context.GetArgument<string>(nameof(UserId));
        OrganizationId = context.GetCurrentOrganizationId();
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
        CultureName = context.GetArgument<string>(nameof(CultureName));

        LineItemIds = context.GetArgument<IList<string>>(nameof(LineItemIds));
    }
}
