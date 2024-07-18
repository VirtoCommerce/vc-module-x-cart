using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Queries
{
    public class GetCartQuery : Query<CartAggregate>, ICartQuery
    {
        public string CartId { get; set; }
        public string StoreId { get; set; }
        public string CartType { get; set; }
        public string CartName { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string CurrencyCode { get; set; }
        public string CultureName { get; set; }

        public IList<string> IncludeFields { get; set; } = Array.Empty<string>();

        public override IEnumerable<QueryArgument> GetArguments()
        {
            yield return Argument<StringGraphType>(nameof(CartId));
            yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
            yield return Argument<NonNullGraphType<StringGraphType>>(nameof(CurrencyCode), description: "Currency code (\"USD\")");
            yield return Argument<StringGraphType>(nameof(CartType), description: "Cart type");
            yield return Argument<StringGraphType>(nameof(CartName), description: "Cart name");
            yield return Argument<StringGraphType>(nameof(UserId), description: "User Id");
            yield return Argument<StringGraphType>(nameof(CultureName), description: "Culture name (\"en-Us\")");
        }

        public override void Map(IResolveFieldContext context)
        {
            CartId = context.GetArgument<string>(nameof(CartId));
            StoreId = context.GetArgument<string>(nameof(StoreId));
            CartType = context.GetArgument<string>(nameof(CartType));
            CartName = context.GetArgument<string>(nameof(CartName));
            UserId = context.GetArgument<string>(nameof(UserId));
            OrganizationId = context.GetCurrentOrganizationId();
            CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
            CultureName = context.GetArgument<string>(nameof(CultureName));

            IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
        }
    }
}
