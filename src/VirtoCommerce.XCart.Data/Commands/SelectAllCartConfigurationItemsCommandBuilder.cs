using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class SelectAllCartConfigurationItemsCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLockService distributedLockService,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeAllCartConfigurationItemsSelectedCommand, InputChangeAllCartConfigurationItemsSelectedType>(
        authorizationService, distributedLockService, cartRepository)
{
    protected override string Name => "selectAllCartConfigurationItems";

    protected override ChangeAllCartConfigurationItemsSelectedCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.SelectedForCheckout = true;

        return request;
    }
}
