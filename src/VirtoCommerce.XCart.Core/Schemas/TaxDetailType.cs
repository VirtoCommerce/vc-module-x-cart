using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Extensions;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class TaxDetailType : ExtendableGraphType<TaxDetail>
    {
        public TaxDetailType()
        {
            Field<NonNullGraphType<MoneyType>>("amount")
                .Description("Amount")
                .Resolve(context => context.Source.Amount.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("price")
                .Description("Price")
                .Resolve(context => context.Source.Rate.ToMoney(context.GetCart().Currency));
            Field<NonNullGraphType<MoneyType>>("rate")
                .Description("Rate")
                .Resolve(context => context.Source.Rate.ToMoney(context.GetCart().Currency));
            Field<StringGraphType>("name")
                .Description("Name")
                .Resolve(context => context.Source.Name);
        }
    }
}
