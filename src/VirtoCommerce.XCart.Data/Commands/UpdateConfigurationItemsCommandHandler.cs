using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemsCommandHandler : CartCommandHandler<UpdateConfigurationItemsCommand>
{
    private readonly IProductConfigurationSearchService _productConfigurationSearchService;

    public UpdateConfigurationItemsCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        IProductConfigurationSearchService productConfigurationSearchService)
        : base(cartAggregateRepository)
    {
        _productConfigurationSearchService = productConfigurationSearchService;
    }

    public override async Task<CartAggregate> Handle(UpdateConfigurationItemsCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        var lineItem = cartAggregate.GetConfiguredLineItem(request.LineItemId);
        if (lineItem is not null)
        {
            await _productConfigurationSearchService.UpdateSectionsFromCatalogAsync(lineItem.ProductId, request.ConfigurationSections);
        }

        await cartAggregate.UpdateConfigurationItemsAsync(request.LineItemId, request.ConfigurationSections);

        return await SaveCartAsync(cartAggregate);
    }
}
