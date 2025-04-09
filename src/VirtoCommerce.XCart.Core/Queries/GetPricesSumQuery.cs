using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetPricesSumQuery : Query<ExpPricesSum>
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public string CultureName { get; set; }

    public string CurrencyCode { get; set; }

    public IList<string> ProductIds { get; set; } = Array.Empty<string>();

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
        yield return Argument<StringGraphType>(nameof(UserId), description: "User Id");
        yield return Argument<StringGraphType>(nameof(CultureName), description: "Currency code (\"USD\")");
        yield return Argument<StringGraphType>(nameof(CurrencyCode), description: "Culture name (\"en-US\")");

        yield return Argument<NonNullGraphType<ListGraphType<StringGraphType>>>(nameof(ProductIds), description: "Products Id");
    }

    public override void Map(IResolveFieldContext context)
    {
        OrganizationId = context.GetCurrentOrganizationId();
        StoreId = context.GetArgument<string>(nameof(StoreId));
        UserId = context.GetArgument<string>(nameof(UserId)) ?? context.GetCurrentUserId();
        CultureName = context.GetArgument<string>(nameof(CultureName));
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
        ProductIds = context.GetArgument<IList<string>>(nameof(ProductIds));
    }
}
