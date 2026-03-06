using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class AddConfigurationItemsCommandBuilder(
    IMediator mediator,
    IAuthorizationService authorizationService,
    IDistributedLockService distributedLockService,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<AddConfigurationItemsCommand, InputAddConfigurationItemsType>(
        mediator, authorizationService, distributedLockService, cartRepository)
{
    protected override string Name => "addConfigurationItems";
}
