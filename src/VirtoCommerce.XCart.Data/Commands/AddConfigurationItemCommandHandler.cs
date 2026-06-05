using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class AddConfigurationItemCommandHandler : CartCommandHandler<AddConfigurationItemCommand>
{
    private readonly IProductConfigurationSearchService _productConfigurationSearchService;

    public AddConfigurationItemCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        IProductConfigurationSearchService productConfigurationSearchService)
        : base(cartAggregateRepository)
    {
        _productConfigurationSearchService = productConfigurationSearchService;
    }

    public override async Task<CartAggregate> Handle(AddConfigurationItemCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        var lineItem = cartAggregate.GetConfiguredLineItem(request.LineItemId);
        if (lineItem is not null)
        {
            await _productConfigurationSearchService.UpdateSectionsFromCatalogAsync(lineItem.ProductId, [request.ConfigurationSection]);
        }

        await cartAggregate.AddConfigurationItemAsync(request.LineItemId, request.ConfigurationSection);

        return await SaveCartAsync(cartAggregate);
    }
}
