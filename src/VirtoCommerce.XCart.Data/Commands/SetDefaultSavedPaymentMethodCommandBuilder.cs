using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class SetDefaultSavedPaymentMethodCommandBuilder(IAuthorizationService authorizationService)
    : SavedPaymentMethodCommandBuilder<SetDefaultSavedPaymentMethodCommand, InputSavedPaymentMethodIdType>(authorizationService)
{
    protected override string Name => "setDefaultSavedPaymentMethod";
}
