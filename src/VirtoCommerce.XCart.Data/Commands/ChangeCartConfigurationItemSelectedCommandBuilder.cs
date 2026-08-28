using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class ChangeCartConfigurationItemSelectedCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLockService distributedLockService,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeCartConfigurationItemSelectedCommand, InputChangeCartConfigurationItemSelectedType>(
        authorizationService, distributedLockService, cartRepository)
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public ChangeCartConfigurationItemSelectedCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IDistributedLockService distributedLockService,
        ICartAggregateRepository cartRepository)
        : this(authorizationService, distributedLockService, cartRepository)
    {
    }

    protected override string Name => "changeCartConfigurationItemSelected";
}
