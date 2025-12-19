using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddWishlistBulkItemCommandHandler : IRequestHandler<AddWishlistBulkItemCommand, BulkCartAggregateResult>
    {
        private readonly ICartAggregateRepository _cartAggregateRepository;
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;
        private readonly ICartProductService _cartProductService;
        private readonly IMediator _mediator;

        public AddWishlistBulkItemCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            IProductConfigurationSearchService productConfigurationSearchService,
            ICartProductService cartProductService,
            IMediator mediator)
        {
            _cartAggregateRepository = cartAggregateRepository;
            _productConfigurationSearchService = productConfigurationSearchService;
            _cartProductService = cartProductService;
            _mediator = mediator;
        }

        public virtual async Task<BulkCartAggregateResult> Handle(AddWishlistBulkItemCommand request, CancellationToken cancellationToken)
        {
            var result = new BulkCartAggregateResult();

            foreach (var listId in request.ListIds)
            {
                var addWishlistItemCommand = new AddWishlistItemCommand
                {
                    ListId = listId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity ?? 1,
                    ConfigurationSections = request.ConfigurationSections,
                };

                var cartAggregate = await _mediator.Send(addWishlistItemCommand, cancellationToken);

                result.CartAggregates.Add(cartAggregate);
            }

            return result;
        }
    }
}
