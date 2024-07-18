using System.Collections.Generic;
using MediatR;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddWishlistBulkItemCommand : IRequest<BulkCartAggregateResult>
    {
        public string ProductId { get; set; }
        public int? Quantity { get; set; }

        public IList<string> ListIds { get; set; }
    }
}
