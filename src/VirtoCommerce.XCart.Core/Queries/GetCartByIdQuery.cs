using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Core.Queries
{
    public class GetCartByIdQuery : IQuery<CartAggregate>
    {
        public string CartId { get; set; }
    }
}
