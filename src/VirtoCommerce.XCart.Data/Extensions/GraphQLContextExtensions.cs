using GraphQL;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Data.Schemas;

namespace VirtoCommerce.XCart.Data.Extensions
{
    public static class GraphQLContextExtensions
    {
        public static T GetCartCommand<T>(this IResolveFieldContext<CartAggregate> context)
            where T : CartCommand => (T)context.GetArgument(GenericTypeHelper.GetActualType<T>(), PurchaseSchema.CommandName);
    }
}
