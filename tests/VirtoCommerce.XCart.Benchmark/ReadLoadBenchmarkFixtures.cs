using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data.Queries;
using VirtoCommerce.XCart.Data.Services;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Fixture helpers for the READ / LOAD / VALIDATION benchmark cluster.
///
/// Shared-fixture limitations worked around here (FLAG for centralising):
///   1. <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> serves the mutate-existing-cart
///      harness correctly; we reuse it for the query handlers too (both load via the repo's
///      <c>GetCartByIdAsync</c> → never-cache → fresh-cart path).
///   2. <see cref="BuildValidationContextFactory"/> creates a working
///      <see cref="ICartValidationContextFactory"/> locally (not in the shared file) that provides
///      <c>AllCartProducts</c> from the cart's own line items so <c>CartValidator</c>'s per-item
///      rules run on real data. The shared fixture's <c>CartBenchmarkFixtures.CreateAggregate</c>
///      uses a loose <c>Mock.Of&lt;ICartValidationContextFactory&gt;()</c> — sufficient for mutation
///      benchmarks that never call <c>ValidateAsync</c> but wrong for the validation subject.
///   3. <see cref="BuildValidateCartAggregate"/> builds an aggregate with a working context factory
///      by reconstructing it from pieces (real calculator + real context factory) rather than
///      forwarding <c>CreateAggregate</c> (which takes a loose mock) — the only way to inject a
///      working factory without editing the shared fixture.
/// </summary>
internal static class ReadLoadBenchmarkFixtures
{
    // ── GetCart query handler ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="GetCartQueryHandler"/> over the shared mutate-existing-cart harness
    /// (which provides a never-cache <see cref="CartAggregateRepository"/> that loads a fresh cart
    /// and runs the real recalc on every call).
    /// </summary>
    public static GetCartQueryHandler CreateGetCartHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);

        // CartResponseGroupParser is stateless — instantiate the real one.
        var parser = new CartResponseGroupParser();

        return new GetCartQueryHandler(harness.Repository, parser);
    }

    /// <summary>
    /// A <c>getCart</c> query that resolves by CartId (the CartId load path — same path the
    /// mutation handlers use, so the harness's fresh-cart mock is exercised).
    /// </summary>
    public static GetCartQuery CreateGetCartQuery() =>
        new()
        {
            CartId = "benchmark-cart",
            StoreId = CartBenchmarkFixtures.StoreId,
            CurrencyCode = CartBenchmarkFixtures.Currency.Code,
            CultureName = "en-US",
            UserId = "benchmark-user",
            // IncludeFields left empty → parser yields Full response group (no dynamic-props strip).
        };

    // ── GetPricesSum query handler ────────────────────────────────────────────────────────────

    /// <summary>
    /// Real <see cref="GetPricesSumQueryHandler"/> over the shared mutate-existing-cart harness.
    ///
    /// The handler's flow: (1) load the source cart by CartId, (2) create a new temp aggregate
    /// via <c>GetCartForShoppingCartAsync</c>, (3) copy items → <c>AddItemsAsync</c> on the temp
    /// aggregate, (4) <c>RecalculateAsync</c> on the temp aggregate, (5) read totals.
    ///
    /// The repository's <c>GetCartForShoppingCartAsync</c> goes through
    /// <c>InnerGetCartAggregateFromCartAsync</c> (never-cache miss → real build + recalc per call).
    /// Both cart loads use the same never-cache mock, so each invocation exercises two real
    /// recalculates (one for the source load, one inside the handler after copy).
    /// </summary>
    public static GetPricesSumQueryHandler CreateGetPricesSumHandler(int lineItemCount, CartShape shape)
    {
        var harness = CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape);
        return new GetPricesSumQueryHandler(harness.Repository);
    }

    /// <summary>
    /// A <c>getPricesSum</c> query whose <see cref="GetPricesSumQuery.LineItemIds"/> covers all
    /// line items of the loaded cart so the copy + recalc path is fully exercised.
    /// </summary>
    public static GetPricesSumQuery CreateGetPricesSumQuery(int lineItemCount) =>
        new()
        {
            CartId = "benchmark-cart",
            StoreId = CartBenchmarkFixtures.StoreId,
            CurrencyCode = CartBenchmarkFixtures.Currency.Code,
            CultureName = "en-US",
            UserId = "benchmark-user",
            // All line-item ids from the fixture cart (li-0 … li-{n-1}).
            LineItemIds = Enumerable.Range(0, lineItemCount).Select(i => $"li-{i}").ToList(),
        };

    // ── ValidateCart aggregate-direct ──────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="CartAggregate"/> that has a WORKING <see cref="ICartValidationContextFactory"/>
    /// so <see cref="CartAggregate.ValidateAsync(string)"/> → <see cref="CartValidator"/> runs the
    /// full per-item rule set on real data (not on a null/empty products list from a loose mock).
    ///
    /// The context factory here is a minimal mock that:
    ///   - Returns <c>AllCartProducts</c> built from the aggregate's own line items (active + buyable
    ///     + priced) — the same shape <see cref="CartBenchmarkFixtures.CreateCartProduct"/> produces.
    ///   - Leaves <c>AvailPaymentMethods</c> / <c>AvailShippingRates</c> empty (the benchmark cart
    ///     has no payments or shipments, so those rule sets don't fire unless requested).
    ///
    /// FLAG for centralising: this is a local work-around because <c>CartBenchmarkFixtures.CreateAggregate</c>
    /// uses a loose <c>Mock.Of&lt;ICartValidationContextFactory&gt;()</c>. Adding a
    /// <c>CartBenchmarkFixtures.CreateAggregateWithValidation()</c> overload would let all clusters
    /// share this factory mock.
    /// </summary>
    public static CartAggregate BuildValidateCartAggregate(int lineItemCount, CartShape shape)
    {
        // Build the working context factory before creating the aggregate (we need its reference).
        var contextFactory = BuildValidationContextFactory();

        var aggregate = BuildAggregateWithContextFactory(contextFactory);

        var cart = CartBenchmarkFixtures.CreateCart(lineItemCount, shape);
        aggregate.GrabCart(cart, CartBenchmarkFixtures.CreateStore(), member: null, CartBenchmarkFixtures.Currency);

        // Settle totals synchronously — GlobalSetup cannot await.
        aggregate.RecalculateAsync().GetAwaiter().GetResult();

        return aggregate;
    }

    /// <summary>
    /// The ruleSet string used by <c>CartType.validationErrors</c> resolver:
    /// <c>aggregate.ValidateAsync(ruleSet)</c> where <c>ruleSet</c> comes from the
    /// <c>ruleSet</c> argument defaulting to <see cref="CartAggregate.ValidationRuleSet"/>
    /// joined by comma.
    ///
    /// Default is <c>"{default},{strict}"</c> which matches <c>ValidationRuleSet</c>'s default
    /// value of <c>["default", "strict"]</c>.
    ///
    /// The GraphQL field also fires the per-item sub-validator via the <c>"items"</c> rule set
    /// when validating the whole cart (see <see cref="CartValidator"/> RuleSets). We benchmark
    /// the <c>Items</c> rule set explicitly because it is the per-line-item hot path.
    /// </summary>
    public const string ItemsRuleSet = ModuleConstants.ValidationRuleSets.Items;

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    private static Mock<ICartValidationContextFactory> BuildValidationContextFactory()
    {
        var mock = new Mock<ICartValidationContextFactory>();

        // ValidateAsync(string) calls CreateValidationContextAsync(CartAggregate) (one-arg).
        // Populate AllCartProducts from the aggregate's line items so per-item rules have
        // real CartProduct instances (IsActive/IsBuyable/priced) and don't short-circuit.
        mock.Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync((CartAggregate aggregate) =>
                new CartValidationContext
                {
                    CartAggregate = aggregate,
                    AllCartProducts = aggregate.LineItems
                        .Select(li => CartBenchmarkFixtures.CreateCartProduct(li.ProductId))
                        .ToList(),
                });

        return mock;
    }

    private static CartAggregate BuildAggregateWithContextFactory(Mock<ICartValidationContextFactory> contextFactory)
    {
        // Re-assemble the CartAggregate with the real totals calculator and a working context
        // factory. All other deps are either mocked to their safe-default shape or are loose mocks
        // (same as CartBenchmarkFixtures.CreateAggregate).
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetAllCurrenciesAsync()).ReturnsAsync([CartBenchmarkFixtures.Currency]);

        var marketingEvaluator = new Mock<VirtoCommerce.MarketingModule.Core.Services.IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<VirtoCommerce.MarketingModule.Core.Model.Promotions.PromotionEvaluationContext>()))
            .ReturnsAsync(new VirtoCommerce.MarketingModule.Core.Model.Promotions.PromotionResult());

        var configItemValidator = new Mock<IConfigurationItemValidator>();
        configItemValidator
            .Setup(x => x.ValidateAsync(It.IsAny<LineItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var totalsCalculator = new VirtoCommerce.CartModule.Data.Services.DefaultShoppingCartTotalsCalculator(currencyService.Object);

        var context = new CartAggregateContext(
            marketingEvaluator.Object,
            totalsCalculator,
            Mock.Of<IOptionalDependency<ITaxProviderSearchService>>(),
            Mock.Of<ICartProductService>(),
            Mock.Of<IDynamicPropertyUpdaterService>(),
            Mock.Of<AutoMapper.IMapper>(),
            Mock.Of<IMemberService>(),
            configItemValidator.Object,
            Mock.Of<IFileUploadService>(),
            Mock.Of<ICartSharingService>(),
            contextFactory.Object);

        return BenchmarkEnvironment.Current.CreateAggregate(context);
    }
}
