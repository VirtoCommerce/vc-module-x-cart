using System.Collections.Generic;
using Moq;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared fixture builders for the Gifts / SavedForLater / DynamicProperties benchmark cluster.
/// Follows the same design rule as <see cref="CartBenchmarkFixtures"/>: everything that does I/O is
/// mocked at the leaf, pure compute runs for real.
///
/// <b>Gifts path</b>: <see cref="ICartAvailMethodsService.GetAvailableGiftsAsync"/> is mocked to
/// return an empty gift list. The shared marketing evaluator in
/// <see cref="CartBenchmarkFixtures.CreateAggregate"/> also returns an empty
/// <see cref="VirtoCommerce.MarketingModule.Core.Model.Promotions.PromotionResult"/> (no rewards).
/// Therefore both <c>AddGiftItemsAsync</c> and <c>RejectCartItems</c> exercise the load+recalc
/// load path and immediately short-circuit the gift-list scan (empty list / no matching gift items
/// on the cart). This measures the <b>empty-gift success path</b> — the baseline cost a promotion
/// miss pays every request. To measure the add path with real rewards, a fixture would need a custom
/// marketing evaluator returning non-empty rewards AND matching gift IDs; that is out of scope for
/// the baseline cluster (add a separate benchmark if regression tracking of the add path is needed).
///
/// <b>SavedForLater path</b>: <see cref="ISavedForLaterListService"/> is mocked entirely. The
/// handler is a thin pass-through to the service, which internally calls
/// <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/> multiple times (load
/// cart, search/create saved-for-later list, save both). Mocking the service measures the MediatR
/// dispatch + handler overhead only. FLAG: if a full-path benchmark (including the list search and
/// double-save) is desired, a deeper harness wiring the real
/// <see cref="VirtoCommerce.XCart.Data.Services.SavedForLaterListService"/> over two
/// <see cref="CartBenchmarkFixtures.MutationHarness"/> instances would be required.
///
/// <b>DynamicProperties path</b>: <see cref="IDynamicPropertyUpdaterService"/> is a loose mock
/// (returns completed task, no-op). <see cref="UpdateCartDynamicPropertiesCommand.DynamicProperties"/>
/// carries one <see cref="DynamicPropertyValue"/> so the delegate in the aggregate is exercised. The
/// measured cost is: load cart (real build + recalc), delegate to the no-op updater, save (recalc
/// again). Only shape matters for the load/save path cost; the updater itself is zero overhead.
/// </summary>
internal static class GiftsSavedDynamicBenchmarkFixtures
{
    // ── Gifts ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="AddGiftItemsCommandHandler"/> over the shared mutation harness. The
    /// <see cref="ICartAvailMethodsService"/> mock returns an empty gift list so the handler
    /// exercises the load+recalc overhead with an empty available-gift scan (no gifts added).
    /// See class-level doc for rationale.
    /// </summary>
    public static AddGiftItemsCommandHandler CreateAddGiftItemsHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        // Return an empty available-gift list — AddGiftItemsAsync ignores IDs not in the list.
        var availMethodsService = new Mock<ICartAvailMethodsService>();
        availMethodsService
            .Setup(x => x.GetAvailableGiftsAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync([]);

        return new AddGiftItemsCommandHandler(harness.Repository, availMethodsService.Object);
    }

    /// <summary>
    /// An <c>addGiftItems</c> command with an empty <c>Ids</c> collection. Since the shared
    /// available-gift list is also empty, passing IDs would produce the same no-op outcome; an
    /// empty list short-circuits the inner loop in <see cref="CartAggregate.AddGiftItemsAsync"/>
    /// before any lookup, making the command data self-consistent and non-throwing.
    /// </summary>
    public static AddGiftItemsCommand CreateAddGiftItemsCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddGiftItemsCommand
        {
            Ids = [],
        });

    /// <summary>
    /// Real <see cref="RejectGiftCartItemsCommandHandler"/> over the shared mutation harness. The
    /// loaded cart has no gift items (<see cref="CartAggregate.GiftItems"/> filters on
    /// <c>IsGift == true</c>; the fixture cart carries plain line items only), so
    /// <see cref="CartAggregate.RejectCartItems"/> scans an empty sequence and returns immediately —
    /// the success no-op path. The measured cost is load+recalc + empty-scan + save (recalc again).
    /// </summary>
    public static RejectGiftCartItemsCommandHandler CreateRejectGiftCartItemsHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>
    /// A <c>rejectGiftCartItems</c> command with an empty <c>Ids</c> collection. The loaded cart
    /// has no gift items so any ID would also be a no-op; an empty list short-circuits the inner
    /// loop in <see cref="CartAggregate.RejectCartItems"/> before the scan.
    /// </summary>
    public static RejectGiftCartItemsCommand CreateRejectGiftCartItemsCommand() =>
        CartBenchmarkFixtures.WithCartContext(new RejectGiftCartItemsCommand
        {
            Ids = [],
        });

    // ── SavedForLater ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stamps context onto any <see cref="MoveSavedForLaterItemsCommandBase"/> (covariant helper so
    /// both To/From commands are covered without duplicating the stamp logic).
    /// </summary>
    private static void StampMoveContext(MoveSavedForLaterItemsCommandBase command)
    {
        command.CartId = "benchmark-cart";
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.LineItemIds = ["li-0"];
    }

    /// <summary>
    /// Builds a <see cref="CartAggregateWithList"/> result whose two aggregates carry empty carts.
    /// The mock service returns this to keep the handler's return path non-null.
    /// </summary>
    private static Mock<ISavedForLaterListService> CreateSavedForLaterServiceMock()
    {
        var mock = new Mock<ISavedForLaterListService>();

        var emptyResult = new CartAggregateWithList
        {
            Cart = null,
            List = null,
        };

        mock.Setup(x => x.MoveToSavedForLaterItems(It.IsAny<MoveSavedForLaterItemsCommandBase>()))
            .ReturnsAsync(emptyResult);

        mock.Setup(x => x.MoveFromSavedForLaterItems(It.IsAny<MoveSavedForLaterItemsCommandBase>()))
            .ReturnsAsync(emptyResult);

        return mock;
    }

    /// <summary>
    /// Real <see cref="MoveToSavedForLaterItemsCommandHandler"/> with a mocked
    /// <see cref="ISavedForLaterListService"/>. The handler is a thin pass-through to the service;
    /// the mock measures the MediatR dispatch + handler overhead only (no repository I/O).
    /// See class-level doc for the FLAG.
    /// </summary>
    public static MoveToSavedForLaterItemsCommandHandler CreateMoveToSavedForLaterHandler() =>
        new(CreateSavedForLaterServiceMock().Object);

    /// <summary>A <c>moveToSavedForLater</c> command targeting the first line item.</summary>
    public static MoveToSavedForLaterItemsCommand CreateMoveToSavedForLaterCommand()
    {
        var cmd = new MoveToSavedForLaterItemsCommand();
        StampMoveContext(cmd);

        return cmd;
    }

    /// <summary>
    /// Real <see cref="MoveFromSavedForLaterItemsCommandHandler"/> with a mocked
    /// <see cref="ISavedForLaterListService"/>. Same rationale as
    /// <see cref="CreateMoveToSavedForLaterHandler"/>.
    /// </summary>
    public static MoveFromSavedForLaterItemsCommandHandler CreateMoveFromSavedForLaterHandler() =>
        new(CreateSavedForLaterServiceMock().Object);

    /// <summary>A <c>moveFromSavedForLater</c> command targeting the first line item.</summary>
    public static MoveFromSavedForLaterItemsCommand CreateMoveFromSavedForLaterCommand()
    {
        var cmd = new MoveFromSavedForLaterItemsCommand();
        StampMoveContext(cmd);

        return cmd;
    }

    // ── DynamicProperties ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A minimal list of one <see cref="DynamicPropertyValue"/> — enough to exercise the
    /// <see cref="IDynamicPropertyUpdaterService"/> delegate call without modelling a full dynamic
    /// property schema. The updater is a loose mock (no-op), so the value content is irrelevant;
    /// the non-empty list ensures the aggregate's delegate is reached.
    /// </summary>
    private static IList<DynamicPropertyValue> CreateDynamicPropertyValues() =>
    [
        new DynamicPropertyValue { Name = "benchmark-prop", Value = "benchmark-value" },
    ];

    /// <summary>
    /// Real <see cref="UpdateCartDynamicPropertiesCommandHandler"/> over the shared mutation
    /// harness. The <see cref="IDynamicPropertyUpdaterService"/> inside the aggregate is a loose
    /// mock (returns completed task); the measured cost is load+recalc + no-op updater delegate +
    /// save (recalc again).
    /// </summary>
    public static UpdateCartDynamicPropertiesCommandHandler CreateUpdateCartDynamicPropertiesHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>
    /// An <c>updateCartDynamicProperties</c> command with one dynamic property value.
    /// </summary>
    public static UpdateCartDynamicPropertiesCommand CreateUpdateCartDynamicPropertiesCommand() =>
        CartBenchmarkFixtures.WithCartContext(new UpdateCartDynamicPropertiesCommand
        {
            DynamicProperties = CreateDynamicPropertyValues(),
        });

    /// <summary>
    /// Real <see cref="UpdateCartItemDynamicPropertiesCommandHandler"/> over the shared mutation
    /// harness. Targets the first line item (<c>li-0</c>) — present in every fixture cart
    /// regardless of size. The updater is a loose mock (no-op).
    /// </summary>
    public static UpdateCartItemDynamicPropertiesCommandHandler CreateUpdateCartItemDynamicPropertiesHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>
    /// An <c>updateCartItemDynamicProperties</c> command targeting the first line item with one
    /// dynamic property value.
    /// </summary>
    public static UpdateCartItemDynamicPropertiesCommand CreateUpdateCartItemDynamicPropertiesCommand() =>
        CartBenchmarkFixtures.WithCartContext(new UpdateCartItemDynamicPropertiesCommand
        {
            LineItemId = "li-0",
            DynamicProperties = CreateDynamicPropertyValues(),
        });
}
