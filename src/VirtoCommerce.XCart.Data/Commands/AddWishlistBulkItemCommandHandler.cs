using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddWishlistBulkItemCommandHandler : IRequestHandler<AddWishlistBulkItemCommand, BulkCartAggregateResult>
    {
        private readonly IMediator _mediator;

        public AddWishlistBulkItemCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public virtual async Task<BulkCartAggregateResult> Handle(AddWishlistBulkItemCommand request, CancellationToken cancellationToken)
        {
            var result = AbstractTypeFactory<BulkCartAggregateResult>.TryCreateInstance();

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
