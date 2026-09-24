using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemsCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLock distributedLock,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<UpdateConfigurationItemsCommand, InputUpdateConfigurationItemsType>(
        authorizationService, distributedLock, cartRepository)
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public UpdateConfigurationItemsCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IDistributedLock distributedLock,
        ICartAggregateRepository cartRepository)
        : this(authorizationService, distributedLock, cartRepository)
    {
    }

    protected override string Name => "updateConfigurationItems";
}
