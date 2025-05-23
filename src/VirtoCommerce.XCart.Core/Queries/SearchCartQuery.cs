using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries
{
    public class SearchCartQuery : SearchQuery<SearchCartResponse>, ICartQuery
    {
        public string StoreId { get; set; }
        public string CartType { get; set; }
        public string CartName { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string CurrencyCode { get; set; }
        public string CultureName { get; set; }
        public string Filter { get; set; }

        public IList<string> IncludeFields { get; set; } = Array.Empty<string>();

        public override IEnumerable<QueryArgument> GetArguments()
        {
            yield return Argument<StringGraphType>(nameof(Sort));
            yield return Argument<StringGraphType>(nameof(StoreId));
            yield return Argument<StringGraphType>(nameof(UserId));
            yield return Argument<StringGraphType>(nameof(CurrencyCode));
            yield return Argument<StringGraphType>(nameof(CultureName));
            yield return Argument<StringGraphType>(nameof(CartType));
            yield return Argument<StringGraphType>(nameof(Filter));
        }

        public override void Map(IResolveFieldContext context)
        {
            base.Map(context);

            StoreId = context.GetArgument<string>(nameof(StoreId));
            UserId = context.GetArgument<string>(nameof(UserId));
            OrganizationId = context.GetCurrentOrganizationId();
            CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
            CultureName = context.GetArgument<string>(nameof(CultureName));
            CartType = context.GetArgument<string>(nameof(CartType));
            Filter = context.GetArgument<string>(nameof(Filter));

            IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
        }
    }
}
