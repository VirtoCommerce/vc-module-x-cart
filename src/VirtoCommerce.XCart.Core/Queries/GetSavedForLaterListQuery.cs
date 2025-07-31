using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries
{
    public partial class GetSavedForLaterListQuery : Query<CartAggregate>, ICartRequest
    {
        public string StoreId { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string CurrencyCode { get; set; }
        public string CultureName { get; set; }

        //For ICartRequest support only
        public string CartType { get; set; }
        public string CartName { get; set; }

        public override IEnumerable<QueryArgument> GetArguments()
        {
            yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), description: "Store Id");
            yield return Argument<NonNullGraphType<StringGraphType>>(nameof(UserId), description: "Customer Id");
            yield return Argument<StringGraphType>(nameof(OrganizationId), description: "Organization Id");
            yield return Argument<StringGraphType>(nameof(CurrencyCode), description: "Currency code (\"USD\")");
            yield return Argument<StringGraphType>(nameof(CultureName), description: "Culture name (\"en-US\")");
        }

        public override void Map(IResolveFieldContext context)
        {
            StoreId = context.GetArgument<string>(nameof(StoreId));
            UserId = context.GetArgument<string>(nameof(UserId));
            OrganizationId = context.GetArgument<string>(nameof(OrganizationId));
            CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
            CultureName = context.GetArgument<string>(nameof(CultureName));
        }
    }
}
