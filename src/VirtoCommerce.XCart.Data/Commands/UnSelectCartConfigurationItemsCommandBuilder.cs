using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using IDistributedLock = VirtoCommerce.Platform.Core.DistributedLock.IDistributedLock;

namespace VirtoCommerce.XCart.Data.Commands;

public class UnSelectCartConfigurationItemsCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLock distributedLock,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeCartConfigurationItemsSelectedCommand, InputChangeCartConfigurationItemsSelectedType>(
        authorizationService, distributedLock, cartRepository)
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public UnSelectCartConfigurationItemsCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IDistributedLockService distributedLockService,
        ICartAggregateRepository cartRepository)
        : this(authorizationService, new LegacyDistributedLockServiceAdapter(distributedLockService), cartRepository)
    {
    }

    protected override string Name => "unSelectCartConfigurationItems";
}
