using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class CardPaymentMethodType : ExtendableGraphType<CardPaymentMethod>
{
    public CardPaymentMethodType()
    {
        Name = "CardPaymentMethod";
        Description = "A payment method that can save cards, with the public parameters its card form needs.";

        Field(x => x.PaymentMethodCode);
        Field(x => x.Name, nullable: true);
        Field(x => x.LogoUrl, nullable: true);
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<CardPaymentMethodParameterType>>>>("publicParameters")
            .Resolve(context => context.Source.PublicParameters.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList());
    }
}
