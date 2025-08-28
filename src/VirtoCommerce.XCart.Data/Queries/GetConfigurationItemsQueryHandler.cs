using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetConfigurationItemsQueryHandler : IQueryHandler<GetConfigurationItemsQuery, ConfigurationItemsResponse>
{
    private readonly ICartAggregateRepository _cartAggregateRepository;

    public GetConfigurationItemsQueryHandler(ICartAggregateRepository cartAggregateRepository)
    {
        _cartAggregateRepository = cartAggregateRepository;
    }

    public async Task<ConfigurationItemsResponse> Handle(GetConfigurationItemsQuery request, CancellationToken cancellationToken)
    {
        var result = AbstractTypeFactory<ConfigurationItemsResponse>.TryCreateInstance();

        var cartAggregate = await GetCartAggregateAsync(request);
        if (cartAggregate == null)
        {
            return result;
        }

        result.CartAggregate = cartAggregate;

        var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id == request.LineItemId);
        if (lineItem == null)
        {
            return result;
        }

        result.ConfigurationItems = lineItem.ConfigurationItems?.ToArray();

        return result;
    }

    protected virtual Task<CartAggregate> GetCartAggregateAsync(GetConfigurationItemsQuery request)
    {
        if (!string.IsNullOrEmpty(request.CartId))
        {
            return _cartAggregateRepository.GetCartByIdAsync(request.CartId, request.CultureName);
        }
        else
        {
            return _cartAggregateRepository.GetCartAsync(request);
        }
    }
}
