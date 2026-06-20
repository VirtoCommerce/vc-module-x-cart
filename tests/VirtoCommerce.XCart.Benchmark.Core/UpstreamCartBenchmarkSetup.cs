using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The default setup: the un-extended XCart platform. Registers no type overrides (the factory
/// resolves the base cart-graph models via its fallback) and builds the plain
/// <see cref="CartAggregate"/> with a mocked, no-op pipeline launcher — upstream recalculate runs no
/// per-item middleware chain.
/// </summary>
public sealed class UpstreamCartBenchmarkSetup : ICartModuleBenchmarkSetup
{
    public void RegisterTypes()
    {
        // Nothing to register — un-extended XCart has no AbstractTypeFactory overrides for the
        // cart-graph model types, and TryCreateInstance falls back to the concrete base type.
    }

    public CartAggregate CreateAggregate(CartAggregateContext context) =>
        new CartAggregate(
            context.MarketingEvaluator,
            context.TotalsCalculator,
            context.TaxProviderSearchService,
            context.CartProductService,
            context.DynamicPropertyUpdaterService,
            context.Mapper,
            context.MemberService,
            Mock.Of<IGenericPipelineLauncher>(), // upstream recalculate runs no per-item pipeline
            context.ConfigurationItemValidator,
            context.FileUploadService,
            context.CartSharingService,
            context.CartValidationContextFactory);

    /// <summary>
    /// Un-extended XCart: the base <see cref="CartAggregate"/> and a no-op pipeline launcher (upstream
    /// recalculate runs no per-item middleware chain). No <c>AbstractTypeFactory</c> overrides — the
    /// factory resolves the base cart-graph models and commands via its fallback.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Mock.Of<IGenericPipelineLauncher>());
        services.AddTransient<CartAggregate>();
    }
}
