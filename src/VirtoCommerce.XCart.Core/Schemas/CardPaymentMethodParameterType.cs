using System.Collections.Generic;
using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class CardPaymentMethodParameterType : ObjectGraphType<KeyValuePair<string, string>>
{
    public CardPaymentMethodParameterType()
    {
        Name = "CardPaymentMethodParameter";

        Field<NonNullGraphType<StringGraphType>>("key").Resolve(context => context.Source.Key);
        Field<StringGraphType>("value").Resolve(context => context.Source.Value);
    }
}
