using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Events;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.XCart.Core;

namespace Heineken.XapiModule.Data.Handlers;

public class CartChangedEventHandler : IEventHandler<CartChangedEvent>
{
    public Task Handle(CartChangedEvent message)
    {
        var cartIds = message.ChangedEntries
            .Select(x => x.NewEntry?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct();

        foreach (var cartId in cartIds)
        {
            GenericCachingRegion<CartAggregate>.ExpireTokenForKey(cartId);
        }

        return Task.CompletedTask;
    }
}
