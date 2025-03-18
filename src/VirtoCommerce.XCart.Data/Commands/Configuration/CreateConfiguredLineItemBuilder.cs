using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.Configuration;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands.Configuration;

public class CreateConfiguredLineItemBuilder : CommandBuilder<CreateConfiguredLineItemCommand, ExpConfigurationLineItem, InputCreateConfiguredLineItemCommand, ConfigurationLineItemType>
{
    public CreateConfiguredLineItemBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
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
