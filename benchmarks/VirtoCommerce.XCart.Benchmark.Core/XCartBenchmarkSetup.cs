using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model.Search;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Middlewares;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The default setup: the un-extended XCart platform, wired so recalculate runs the same way it does in
/// production — real promotion pipeline AND tax. The base <see cref="CartAggregate"/> is built with a
/// REAL <see cref="IGenericPipelineLauncher"/> (registered by <c>AddPipeline</c>) so the
/// promotion-evaluation pipeline's <see cref="MapPromoEvalContextMiddleware"/> maps the cart into the
/// promo context, the marketing evaluator returns a cart-subtotal reward (reward application runs), and —
/// with the store's tax-calculation setting enabled and an active fixed-rate provider exposed — the
/// tax branch + ApplyTaxRates run. No <c>AbstractTypeFactory</c> overrides — the factory resolves the
/// base cart-graph models and commands via its fallback.
/// </summary>
public sealed class XCartBenchmarkSetup : ICartBenchmarkSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ── Real recalculate promotion pipeline (replaces the no-op mock launcher) ────────────────────
        // AddPipeline TryAdds the real GenericPipelineLauncher, so RecalculateAsync's
        // EvaluatePromotionsAsync executes the production promo-context map. The map's shopper-data load
        // is I/O → mocked; the cart→context mapping (the real cost) runs via the host's real IMapper.
        services.AddSingleton(Mock.Of<ILoadUserToEvalContextService>());
        services.AddPipeline<PromotionEvaluationContextCartMap>(builder =>
            builder.AddMiddleware(typeof(MapPromoEvalContextMiddleware)));
        // Empty pipeline, mirroring production registration: the base AddShipmentAsync drives it, so a
        // real launcher would throw "pipeline not registered" on the shipment benchmark without it.
        services.AddPipeline<ShipmentContextCartMap>();

        // A non-empty promotion result so ApplyRewardsAsync runs the real reward-application path (a
        // cart-subtotal discount → Discounts + DiscountAmount, reflected by the second CalculateTotals).
        // The evaluator is I/O → mocked; the reward stands in for a single active promotion.
        var promotionResult = new PromotionResult();
        promotionResult.Rewards.Add(new CartSubtotalReward { IsValid = true, Amount = 5m, AmountType = RewardAmountType.Absolute });

        var marketingEvaluator = new Mock<IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
            .ReturnsAsync(promotionResult);
        services.AddSingleton(marketingEvaluator.Object);

        // ── Real tax evaluation ───────────────────────────────────────────────────────────────────────
        // The store tax-calculation gate is already satisfied: its SettingDescriptor.DefaultValue is true,
        // so GetValue<bool> on the host's settings-less store returns true. The only thing gating tax off
        // in the base host is the absent tax-search dependency — expose it as present with one active
        // fixed-rate provider so EvaluateTaxesAsync + Cart.ApplyTaxRates run on every recalc.
        var taxSearchService = new Mock<ITaxProviderSearchService>();
        taxSearchService
            .Setup(x => x.SearchAsync(It.IsAny<TaxProviderSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new TaxProviderSearchResult { Results = [new BenchmarkTaxProvider()] });

        var taxDependency = new Mock<IOptionalDependency<ITaxProviderSearchService>>();
        taxDependency.SetupGet(x => x.HasValue).Returns(true);
        taxDependency.SetupGet(x => x.Value).Returns(taxSearchService.Object);
        services.AddSingleton(taxDependency.Object);

        services.AddTransient<CartAggregate>();
    }

    // A fixed-rate (10%) tax provider mirroring the platform FixedRateTaxProvider's CalculateRates without
    // the settings lookup — one TaxRate per evaluated line so ApplyTaxRates runs over the real cart.
    private sealed class BenchmarkTaxProvider : TaxProvider
    {
        public BenchmarkTaxProvider()
        {
            Code = "Benchmark";
            IsActive = true;
        }

        public override IEnumerable<TaxRate> CalculateRates(IEvaluationContext context)
        {
            var taxEvalContext = (TaxEvaluationContext)context;

            foreach (var line in taxEvalContext.Lines)
            {
                var rate = AbstractTypeFactory<TaxRate>.TryCreateInstance();
                rate.Rate = line.Amount * 0.1m;
                rate.Currency = taxEvalContext.Currency;
                rate.TaxProvider = this;
                rate.Line = line;
                yield return rate;
            }
        }
    }
}
