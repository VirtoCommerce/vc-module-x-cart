using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command builders for the Gifts / SavedForLater / DynamicProperties cluster. Handlers and the
/// aggregate are resolved through the DI container (<see cref="CartBenchmarkHost"/>); only the command
/// objects live here.
/// </summary>
internal static class GiftsSavedDynamicBenchmarkFixtures
{
    // ── Gifts ─────────────────────────────────────────────────────────────────────────────────────

    public const string GiftId = "benchmark-gift";

    /// <summary>An <c>addGiftItems</c> command requesting the one available gift by id.</summary>
    public static AddGiftItemsCommand CreateAddGiftItemsCommand()
    {
        var command = AbstractTypeFactory<AddGiftItemsCommand>.TryCreateInstance();
        command.Ids = [GiftId];

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>A <c>rejectGiftCartItems</c> command. The loaded cart carries no gift line items, so the
    /// reject scan finds nothing and returns — the success no-op path over the real load+recalc+save.</summary>
    public static RejectGiftCartItemsCommand CreateRejectGiftCartItemsCommand()
    {
        var command = AbstractTypeFactory<RejectGiftCartItemsCommand>.TryCreateInstance();
        command.Ids = [GiftId];

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── SavedForLater ───────────────────────────────────────────────────────────────────────────────

    private static void StampMoveContext(MoveSavedForLaterItemsCommandBase command)
    {
        command.CartId = "benchmark-cart";
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.LineItemIds = ["li-0"];
    }

    public static MoveToSavedForLaterItemsCommand CreateMoveToSavedForLaterCommand()
    {
        var command = AbstractTypeFactory<MoveToSavedForLaterItemsCommand>.TryCreateInstance();
        StampMoveContext(command);

        return command;
    }

    public static MoveFromSavedForLaterItemsCommand CreateMoveFromSavedForLaterCommand()
    {
        var command = AbstractTypeFactory<MoveFromSavedForLaterItemsCommand>.TryCreateInstance();
        StampMoveContext(command);

        return command;
    }

    // ── DynamicProperties ─────────────────────────────────────────────────────────────────────────

    private static IList<DynamicPropertyValue> CreateDynamicPropertyValues() =>
    [
        new DynamicPropertyValue { Name = "benchmark-prop", Value = "benchmark-value" },
    ];

    public static UpdateCartDynamicPropertiesCommand CreateUpdateCartDynamicPropertiesCommand()
    {
        var command = AbstractTypeFactory<UpdateCartDynamicPropertiesCommand>.TryCreateInstance();
        command.DynamicProperties = CreateDynamicPropertyValues();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    public static UpdateCartItemDynamicPropertiesCommand CreateUpdateCartItemDynamicPropertiesCommand()
    {
        var command = AbstractTypeFactory<UpdateCartItemDynamicPropertiesCommand>.TryCreateInstance();
        command.LineItemId = "li-0";
        command.DynamicProperties = CreateDynamicPropertyValues();

        return CartBenchmarkFixtures.WithCartContext(command);
    }
}
