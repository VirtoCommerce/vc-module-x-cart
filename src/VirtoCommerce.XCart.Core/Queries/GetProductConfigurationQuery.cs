using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetProductConfigurationQuery : Query<ProductConfigurationQueryResponse>
{
    public string ConfigurableProductId { get; set; }

    public string StoreId { get; set; }
    public string UserId { get; set; }
    public string CultureName { get; set; }
    public string CurrencyCode { get; set; }
    public string OrganizationId { get; set; }

    public Store Store { get; set; }

    public IList<string> IncludeFields { get; set; } = Array.Empty<string>();

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(ConfigurableProductId));

        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
        yield return Argument<StringGraphType>(nameof(UserId), description: "User Id");
        yield return Argument<StringGraphType>(nameof(CultureName), description: "Currency code (\"USD\")");
        yield return Argument<StringGraphType>(nameof(CurrencyCode), description: "Culture name (\"en-US\")");
    }

    public override void Map(IResolveFieldContext context)
    {
        ConfigurableProductId = context.GetArgument<string>(nameof(ConfigurableProductId));

        StoreId = context.GetArgument<string>(nameof(StoreId));
        UserId = context.GetArgument<string>(nameof(UserId)) ?? context.GetCurrentUserId();
        OrganizationId = context.GetCurrentOrganizationId();
        CultureName = context.GetArgument<string>(nameof(CultureName));
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
    }
}
