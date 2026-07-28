namespace VirtoCommerce.XCart.Core.Commands.BaseCommands;

public abstract class ScopedWishlistCommand : WishlistCommand
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    // Optional principal the list is shared with (its id space is defined by Scope, e.g. a customer organization id
    // for a customer-targeted scope). Null for the built-in non-targeted scopes (Private/Organization/Anyone).
    public string SharedWithId { get; set; }
}
