namespace VirtoCommerce.XCart.Core.Commands.BaseCommands;

public abstract class ScopedWishlistCommand : WishlistCommand
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }
}
