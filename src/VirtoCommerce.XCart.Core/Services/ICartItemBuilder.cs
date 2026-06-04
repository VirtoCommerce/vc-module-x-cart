using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Services
{
    /// <summary>
    /// Constructs cart items — <see cref="LineItem"/> and <see cref="ConfigurationItem"/> —
    /// during cart mutation.
    /// </summary>
    public interface ICartItemBuilder
    {
        /// <summary>
        /// Mapper context key used by the <c>CreateMap&lt;CartProduct, LineItem&gt;</c> lambda in
        /// <c>CartMappingProfile</c> to look up the resolved builder from
        /// <c>IMappingOperationOptions.Items</c>. <see cref="CartAggregate.AddItemAsync"/>
        /// populates this entry; the lambda reads it via <c>TryGetValue</c>.
        /// </summary>
        public const string MapperContextKey = "cartItemBuilder";

        LineItem Create(CartProduct cartProduct);

        ConfigurationItem Create(string sectionId, string type);
    }
}
