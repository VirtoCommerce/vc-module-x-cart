using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Commands.BaseCommands;

public abstract class ScopedWishlistCommand : WishlistCommand
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    public string SharedWithId { get; set; }

    public IList<string> AddSharedWithIds { get; set; }

    public IList<string> RemoveSharedWithIds { get; set; }

    public string Message { get; set; }
}
