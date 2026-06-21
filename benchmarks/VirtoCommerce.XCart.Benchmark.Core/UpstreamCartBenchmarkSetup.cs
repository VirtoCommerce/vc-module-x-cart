using Moq;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Middlewares;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The default setup: the un-extended XCart platform, wired so recalculate runs the same way it does in
/// production. The base <see cref="CartAggregate"/> is built with a REAL
/// <see cref="IGenericPipelineLauncher"/> (registered by <c>AddPipeline</c>) so the promotion-evaluation
/// pipeline's <see cref="MapPromoEvalContextMiddleware"/> actually maps the cart into the promo context —
/// the allocation/CPU a mocked launcher silently skipped. The marketing evaluator returns a single
/// cart-subtotal reward so the reward-application path runs too. No <c>AbstractTypeFactory</c> overrides —
/// the factory resolves the base cart-graph models and commands via its fallback.
/// </summary>
public sealed class UpstreamCartBenchmarkSetup : ICartModuleBenchmarkSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Real recalculate pipeline (replaces the no-op mock launcher). AddPipeline TryAdds the real
        // GenericPipelineLauncher, so RecalculateAsync's EvaluatePromotionsAsync executes the production
        // promo-context map. The map's shopper-data load is I/O → mocked; the cart→context mapping (the
        // real cost) runs via the host's real IMapper.
        services.AddSingleton(Mock.Of<ILoadUserToEvalContextService>());
        services.AddPipeline<PromotionEvaluationContextCartMap>(builder =>
            builder.AddMiddleware(typeof(MapPromoEvalContextMiddleware)));
        // Empty pipeline, mirroring production registration: the base AddShipmentAsync drives it, so a
        // real launcher would throw "pipeline not registered" on the shipment benchmark without it; no
        // middleware → it no-ops, matching the mocked baseline for that path.
        services.AddPipeline<ShipmentContextCartMap>();

        // A non-empty promotion result so RecalculateAsync's ApplyRewardsAsync runs the real
        // reward-application path (a cart-subtotal discount → Discounts + DiscountAmount, reflected by the
        // second CalculateTotals). The evaluator is I/O → mocked; the reward it returns stands in for a
        // single active promotion.
        var promotionResult = new PromotionResult();
        promotionResult.Rewards.Add(new CartSubtotalReward { IsValid = true, Amount = 5m, AmountType = RewardAmountType.Absolute });

        var marketingEvaluator = new Mock<IMarketingPromoEvaluator>();
        marketingEvaluator
            .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
            .ReturnsAsync(promotionResult);
        services.AddSingleton(marketingEvaluator.Object);

        services.AddTransient<CartAggregate>();
    }
}
