using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        var cartAggregate = await GetCartAggregateAsync(request)
            ?? throw new OperationCanceledException($"Cart not found");

        var lineItem = cartAggregate.Cart.Items.FirstOrDefault(x => x.Id == request.LineItemId)
            ?? throw new OperationCanceledException($"LineIten not found");

        return new ConfigurationItemsResponse
        {
            CartAggregate = cartAggregate,
            ConfigurationItems = lineItem.ConfigurationItems?.ToArray(),
        };
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
