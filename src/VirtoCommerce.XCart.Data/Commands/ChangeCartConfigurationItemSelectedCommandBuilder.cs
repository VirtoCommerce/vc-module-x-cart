using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class ChangeCartConfigurationItemSelectedCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLock distributedLock,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeCartConfigurationItemSelectedCommand, InputChangeCartConfigurationItemSelectedType>(
        authorizationService, distributedLock, cartRepository)
{
    protected override string Name => "changeCartConfigurationItemSelected";
}
