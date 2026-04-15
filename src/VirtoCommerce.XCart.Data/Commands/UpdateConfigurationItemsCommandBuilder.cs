using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemsCommandBuilder(
    IMediator mediator,
    IAuthorizationService authorizationService,
    IDistributedLockService distributedLockService,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<UpdateConfigurationItemsCommand, InputUpdateConfigurationItemsType>(
        mediator, authorizationService, distributedLockService, cartRepository)
{
    protected override string Name => "updateConfigurationItems";
}
