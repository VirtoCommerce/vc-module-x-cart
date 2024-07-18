using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class TaxDetailType : ObjectGraphType<TaxDetail>
    {
        public TaxDetailType()
        {
            Field<NonNullGraphType<MoneyType>>("amount",
                "Amount",
                resolve: context => context.Source.Amount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("price",
                "Price",
                resolve: context => context.Source.Rate.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("rate",
                "Rate",
                resolve: context => context.Source.Rate.ToMoney(context.GetCart().Currency));
            Field<StringGraphType>("name",
                "Name",
                resolve: context => context.Source.Name);
        }
    }
}
