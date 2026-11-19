using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class AddSavedPaymentMethodCommandBuilder(IAuthorizationService authorizationService)
    : SavedPaymentMethodCommandBuilder<AddSavedPaymentMethodCommand, InputAddSavedPaymentMethodType>(authorizationService)
{
    protected override string Name => "addSavedPaymentMethod";
}
