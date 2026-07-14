using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class SelectCartConfigurationItemsCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLockService distributedLockService,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeCartConfigurationItemsSelectedCommand, InputChangeCartConfigurationItemsSelectedType>(
        authorizationService, distributedLockService, cartRepository)
{
    protected override string Name => "selectCartConfigurationItems";

    protected override ChangeCartConfigurationItemsSelectedCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.SelectedForCheckout = true;

        return request;
    }
}
