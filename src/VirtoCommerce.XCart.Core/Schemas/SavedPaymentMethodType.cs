using System;
using GraphQL.Types;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas;

public class SavedPaymentMethodType : ExtendableGraphType<StoredPaymentMethod>
{
    public SavedPaymentMethodType()
    {
        Name = "SavedPaymentMethod";
        Description = "A card saved at the payment provider. Never contains the card number, CVV or provider token.";

        Field(x => x.Id);
        Field(x => x.PaymentMethodCode);
        Field(x => x.Brand, nullable: true);
        Field(x => x.Last4, nullable: true);
        Field(x => x.ExpirationMonth, nullable: true);
        Field(x => x.ExpirationYear, nullable: true);
        Field(x => x.CardholderName, nullable: true);
        Field(x => x.Status);
        Field(x => x.IsDefault);
        Field<NonNullGraphType<BooleanGraphType>>("isExpired").Resolve(context => context.Source.IsExpired(DateTime.UtcNow));
        Field(x => x.CreatedDate);
    }
}
