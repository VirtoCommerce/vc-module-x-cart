using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public class SharingTargetType : ExtendableGraphType<WishlistSharingTarget>
{
    public SharingTargetType()
    {
        Field(x => x.Id, nullable: false).Description("Id the list is shared with (id space defined by scope)");
        Field(x => x.Name, nullable: true).Description("Display name of the target, when the module owning the scope resolves it");
        Field(x => x.Subtitle, nullable: true).Description("Secondary display line of the target (e.g. city and region), when resolved");
        Field(x => x.ImageUrl, nullable: true).Description("Image URL of the target, when resolved");
    }
}
