using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class RemoveSavedPaymentMethodCommandBuilder(IAuthorizationService authorizationService)
    : SavedPaymentMethodCommandBuilder<RemoveSavedPaymentMethodCommand, InputSavedPaymentMethodIdType>(authorizationService)
{
    protected override string Name => "removeSavedPaymentMethod";
}
