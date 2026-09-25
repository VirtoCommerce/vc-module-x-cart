using GraphQL;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands;

public class SelectAllCartConfigurationItemsCommandBuilder(
    IAuthorizationService authorizationService,
    IDistributedLock distributedLock,
    ICartAggregateRepository cartRepository)
    : CartCommandBuilder<ChangeAllCartConfigurationItemsSelectedCommand, InputChangeAllCartConfigurationItemsSelectedType>(
        authorizationService, distributedLock, cartRepository)
{
    protected override string Name => "selectAllCartConfigurationItems";

    protected override ChangeAllCartConfigurationItemsSelectedCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.SelectedForCheckout = true;

        return request;
    }
}
