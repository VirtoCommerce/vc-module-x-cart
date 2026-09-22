using System;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateConfiguredLineItemBuilder : CommandBuilder<CreateConfiguredLineItemCommand, ExpConfigurationLineItem, InputCreateConfiguredLineItemCommand, ConfigurationLineItemType>
{
    public CreateConfiguredLineItemBuilder(IAuthorizationService authorizationService)
        : base(authorizationService)
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public CreateConfiguredLineItemBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
    {
    }

    protected override string Name => "createConfiguredLineItem";

    protected override Task BeforeMediatorSend(IResolveFieldContext<object> context, CreateConfiguredLineItemCommand request)
    {
        request.UserId = context.GetCurrentUserId();
        request.OrganizationId = context.GetCurrentOrganizationId();
        request.EvaluatePromotions = true;

        return base.BeforeMediatorSend(context, request);
    }
}
