using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command and query builders for the wishlist benchmarks. Handlers and the aggregate are resolved
/// through the DI container (<see cref="CartBenchmarkHost"/>); only the command and query objects live
/// here.
///
/// Wishlist commands carry both a <see cref="CartCommand.CartId"/> and a
/// <see cref="WishlistCommand.ListId"/>. On create paths (CreateWishlist, CloneWishlist,
/// CreateCartFromWishlist) <c>CartId</c> is left empty so the handler creates a new cart; on
/// mutate-existing paths the handler loads by <c>ListId</c>.
///
/// Shape: wishlists run over <see cref="CartShape.Flat"/> only.
/// </summary>
internal static class WishlistBenchmarkFixtures
{
    // ── Constants ────────────────────────────────────────────────────────────────────────────────

    public const string WishlistId = "benchmark-wishlist";
    public const string DestinationWishlistId = "benchmark-wishlist-dest";
    public const string WishlistName = "My Wishlist";
    public const string WishlistProductId = "wishlist-product-0";

    // ── WishlistUserContext helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="WishlistUserContext"/> with no scope, so <c>UpdateScopeAsync</c> in
    /// <c>ScopedWishlistCommandHandlerBase</c> skips all branches and neither
    /// <c>EnsureSharingSettings</c> nor <c>SetOwner</c> is called. Safe with a loose-mock
    /// <c>ICartSharingService</c>.
    /// </summary>
    public static WishlistUserContext EmptyWishlistUserContext() =>
        new()
        {
            CurrentUserId = "benchmark-user",
            UserId = "benchmark-user",
            Scope = null,
        };

    // ── Stamp WishlistCommand context ────────────────────────────────────────────────────────────

    /// <summary>
    /// Stamps the shared cart context onto a <see cref="WishlistCommand"/>: both the
    /// <see cref="CartCommand.CartId"/> (used by the repository's cache key) and the
    /// wishlist-specific <see cref="WishlistCommand.ListId"/> (used by handlers'
    /// <c>GetCartByIdAsync</c> calls). For create-path commands, <c>ListId</c> is the SOURCE
    /// wishlist id; <c>CartId</c> is left empty so the handler's
    /// <c>GetOrCreateCartFromCommandAsync</c> takes the create-new branch.
    /// </summary>
    public static T WithWishlistContext<T>(T command, bool isCreatePath = false)
        where T : WishlistCommand
    {
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.WishlistUserContext = EmptyWishlistUserContext();

        if (isCreatePath)
        {
            // Create-path: no CartId (handler creates new), source ListId for clone/fromwishlist
            command.ListId = WishlistId;
        }
        else
        {
            // Mutate-existing-path: GetCartByIdAsync uses ListId to load the cart
            command.CartId = WishlistId;
            command.ListId = WishlistId;
        }

        return command;
    }

    // ── Command builders: create paths ─────────────────────────────────────────────────────────────

    /// <summary>A <c>createWishlist</c> command with a valid list name and no scope (private by
    /// default). No CartId so the handler creates a new cart each invocation.</summary>
    public static CreateWishlistCommand CreateWishlistCommand()
    {
        var command = AbstractTypeFactory<CreateWishlistCommand>.TryCreateInstance();
        command.ListName = WishlistName;

        return WithWishlistContext(command, isCreatePath: true);
    }

    /// <summary>A <c>cloneWishlist</c> command. <c>ListId</c> is the source wishlist id; no
    /// <c>CartId</c> so the handler creates the clone as a new cart.</summary>
    public static CloneWishlistCommand CreateCloneWishlistCommand()
    {
        var command = AbstractTypeFactory<CloneWishlistCommand>.TryCreateInstance();
        command.ListName = WishlistName + " (clone)";

        return WithWishlistContext(command, isCreatePath: true);
    }

    /// <summary>A <c>createCartFromWishlist</c> command whose source is the fixture wishlist.</summary>
    public static CreateCartFromWishlistCommand CreateCartFromWishlistCommand()
    {
        var command = AbstractTypeFactory<CreateCartFromWishlistCommand>.TryCreateInstance();

        return WithWishlistContext(command, isCreatePath: true);
    }

    // ── Command builders: mutate-existing paths ────────────────────────────────────────────────────

    /// <summary>An <c>addWishlistItem</c> command with a valid product ID and default quantity 1.</summary>
    public static AddWishlistItemCommand CreateAddWishlistItemCommand()
    {
        var command = AbstractTypeFactory<AddWishlistItemCommand>.TryCreateInstance();
        command.ProductId = WishlistProductId;
        command.Quantity = 1;

        return WithWishlistContext(command);
    }

    /// <summary>A <c>renameWishlist</c> command with a new list name. The ctor requires both
    /// <c>listId</c> and <c>listName</c>; <see cref="WithWishlistContext{T}"/> overwrites
    /// <c>ListId</c> to <c>WishlistId</c> after construction.</summary>
    public static RenameWishlistCommand CreateRenameWishlistCommand() =>
        WithWishlistContext(new RenameWishlistCommand(WishlistId, WishlistName + " (renamed)"));

    /// <summary>A <c>changeWishlist</c> command that renames the list. No scope → private path (no-op
    /// in UpdateScopeAsync — Scope = null skips all branches).</summary>
    public static ChangeWishlistCommand CreateChangeWishlistCommand()
    {
        var command = AbstractTypeFactory<ChangeWishlistCommand>.TryCreateInstance();
        command.ListName = WishlistName + " (changed)";
        command.Scope = null;

        return WithWishlistContext(command);
    }

    /// <summary>A <c>removeWishlist</c> command for the fixture wishlist. The ctor requires
    /// <c>listId</c>; <see cref="WithWishlistContext{T}"/> overwrites it to <c>WishlistId</c>.</summary>
    public static RemoveWishlistCommand CreateRemoveWishlistCommand() =>
        WithWishlistContext(new RemoveWishlistCommand(WishlistId));

    /// <summary>A <c>removeWishlistItem</c> command targeting the first line item. The benchmark
    /// measures the load + remove + recalc path; the harness returns a fresh cart each call so
    /// the item is always present.</summary>
    public static RemoveWishlistItemCommand CreateRemoveWishlistItemCommand()
    {
        var command = AbstractTypeFactory<RemoveWishlistItemCommand>.TryCreateInstance();
        command.LineItemId = "li-0";

        return WithWishlistContext(command);
    }

    /// <summary>A <c>moveWishlistItem</c> command moving <c>li-0</c> from the source to the destination
    /// wishlist. The ctor requires all three ids; <see cref="WithWishlistContext{T}"/> overwrites
    /// <c>ListId</c> to <c>WishlistId</c> after construction.</summary>
    public static MoveWishlistItemCommand CreateMoveWishlistItemCommand() =>
        WithWishlistContext(new MoveWishlistItemCommand(WishlistId, DestinationWishlistId, "li-0"));

    /// <summary>An <c>updateWishlistItems</c> command updating the quantity of the first item to 3.
    /// The handler loops over the command's Items and calls <c>ChangeItemQuantityAsync</c> per item;
    /// the benchmark covers the single-item update so the item-count axis drives cart size, not
    /// the number of items updated.</summary>
    public static UpdateWishlistItemsCommand CreateUpdateWishlistItemsCommand()
    {
        var command = AbstractTypeFactory<UpdateWishlistItemsCommand>.TryCreateInstance();
        command.Items = [new WishListItem { LineItemId = "li-0", Quantity = 3 }];

        return WithWishlistContext(command);
    }

    // ── Query builders ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A <c>getWishlist</c> query for the fixture wishlist.</summary>
    public static GetWishlistQuery CreateGetWishlistQuery()
    {
        var query = AbstractTypeFactory<GetWishlistQuery>.TryCreateInstance();
        query.ListId = WishlistId;
        query.CultureName = "en-US";
        query.IncludeFields = [];

        return query;
    }

    /// <summary>A <c>searchWishlists</c> query with minimal required fields.</summary>
    public static SearchWishlistQuery CreateSearchWishlistQuery()
    {
        var query = AbstractTypeFactory<SearchWishlistQuery>.TryCreateInstance();
        query.StoreId = CartBenchmarkFixtures.StoreId;
        query.UserId = "benchmark-user";
        query.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        query.CultureName = "en-US";
        query.IncludeFields = [];
        query.Take = 20;

        return query;
    }
}
