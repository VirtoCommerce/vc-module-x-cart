using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class RemoveConfigurationItemCommandBuilder : CommandBuilder<RemoveConfigurationItemCommand, CartAggregate, InputRemoveConfigurationItemType, CartType>
{
    public RemoveConfigurationItemCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override string Name => "removeConfigurationItem";

    protected override Task BeforeMediatorSend(IResolveFieldContext<object> context, RemoveConfigurationItemCommand request)
    {
        request.UserId = context.GetCurrentUserId();
        request.OrganizationId = context.GetCurrentOrganizationId();

        return base.BeforeMediatorSend(context, request);
    }
}
