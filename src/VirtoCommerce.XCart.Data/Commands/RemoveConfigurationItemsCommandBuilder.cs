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

public class RemoveConfigurationItemsCommandBuilder : CommandBuilder<RemoveConfigurationItemsCommand, CartAggregate, InputRemoveConfigurationItemsType, CartType>
{
    public RemoveConfigurationItemsCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override string Name => "removeConfigurationItems";

    protected override Task BeforeMediatorSend(IResolveFieldContext<object> context, RemoveConfigurationItemsCommand request)
    {
        request.UserId = context.GetCurrentUserId();
        request.OrganizationId = context.GetCurrentOrganizationId();

        return base.BeforeMediatorSend(context, request);
    }
}
