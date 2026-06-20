using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Local command builders for the CART STATE mutation cluster:
/// <c>changeCartCurrency</c>, <c>mergeCart</c>, <c>clearCart</c>, <c>refreshCart</c>,
/// <c>changePurchaseOrderNumber</c>, <c>changeComment</c>, <c>createCart</c>.
///
/// Handlers and the aggregate are resolved through the DI container (<see cref="CartBenchmarkHost"/>);
/// only the command objects live here.
/// </summary>
internal static class CartStateBenchmarkFixtures
{
    // ── changeCartCurrency ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <c>changeCartCurrency</c> command targeting the benchmark cart, switching to USD (same
    /// currency as the loaded cart). Same-currency re-price still exercises the full CopyItems code
    /// path — both the flat and configured branches — without requiring a second currency in the mock.
    /// </summary>
    public static ChangeCartCurrencyCommand CreateChangeCartCurrencyCommand()
    {
        var command = AbstractTypeFactory<ChangeCartCurrencyCommand>.TryCreateInstance();
        command.NewCurrencyCode = CartBenchmarkFixtures.Currency.Code; // "USD" — same as loaded cart

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── mergeCart ───────────────────────────────────────────────────────────────────────────────

    private const string SecondCartId = "second-cart";

    /// <summary>
    /// A <c>mergeCart</c> command. DeleteAfterMerge=false avoids a RemoveCartAsync call so the
    /// benchmark measures merge compute only (no delete I/O path).
    /// </summary>
    public static MergeCartCommand CreateMergeCartCommand()
    {
        var command = AbstractTypeFactory<MergeCartCommand>.TryCreateInstance();
        command.SecondCartId = SecondCartId;
        command.DeleteAfterMerge = false;

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── clearCart ────────────────────────────────────────────────────────────────────────────────

    /// <summary>A <c>clearCart</c> command targeting the benchmark cart.</summary>
    public static ClearCartCommand CreateClearCartCommand()
    {
        var command = AbstractTypeFactory<ClearCartCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── refreshCart ──────────────────────────────────────────────────────────────────────────────

    /// <summary>A <c>refreshCart</c> command targeting the benchmark cart.</summary>
    public static RefreshCartCommand CreateRefreshCartCommand()
    {
        var command = AbstractTypeFactory<RefreshCartCommand>.TryCreateInstance();

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── changePurchaseOrderNumber ────────────────────────────────────────────────────────────────

    /// <summary>A <c>changePurchaseOrderNumber</c> command setting a PO number on the benchmark cart.</summary>
    public static ChangePurchaseOrderNumberCommand CreateChangePurchaseOrderNumberCommand()
    {
        var command = AbstractTypeFactory<ChangePurchaseOrderNumberCommand>.TryCreateInstance();
        command.PurchaseOrderNumber = "PO-BENCHMARK-001";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── changeComment (cart-level) ───────────────────────────────────────────────────────────────

    /// <summary>A <c>changeComment</c> command setting a cart-level comment on the benchmark cart.</summary>
    public static ChangeCommentCommand CreateChangeCommentCommand()
    {
        var command = AbstractTypeFactory<ChangeCommentCommand>.TryCreateInstance();
        command.Comment = "Benchmark cart-level comment";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    // ── createCart ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <c>createCart</c> command with NO CartId (so the handler takes the create-new path).
    /// CartId intentionally left empty — WithCartContext is NOT used here (it sets CartId).
    /// </summary>
    public static CreateCartCommand CreateCreateCartCommand()
    {
        var command = AbstractTypeFactory<CreateCartCommand>.TryCreateInstance();
        command.StoreId = CartBenchmarkFixtures.StoreId;
        command.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        command.CultureName = "en-US";
        command.UserId = "benchmark-user";
        command.CartName = "default";
        // CartId intentionally omitted → create-new path

        return command;
    }
}
