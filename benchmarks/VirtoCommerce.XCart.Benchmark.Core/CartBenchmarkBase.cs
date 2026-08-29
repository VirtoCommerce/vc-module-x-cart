using System;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Base for every cart benchmark. The benchmark <b>logic</b> ([Benchmark] methods, [Params]) lives in
/// per-operation abstract subclasses of this type in the Core library; each runner exe defines a
/// concrete subclass (source-generated from its <see cref="BenchmarkSetupAttribute"/>) overriding
/// <see cref="CreateSetup"/> to bake its module setup. README §"Layout and toolchain" has the model.
///
/// <para><see cref="BuildProvider"/> composes the DI container (base XCart handlers + mocked I/O leaves
/// + the module's <see cref="ICartBenchmarkSetup.ConfigureServices"/> overrides); operations
/// resolve <c>IMediator</c> (command/query benchmarks) or <c>Func&lt;CartAggregate&gt;</c>
/// (aggregate-direct benchmarks) from it.</para>
///
/// <para>Three things hold for EVERY benchmark deriving from this type, which is why a subclass
/// documents only what is specific to it — what it seeds, and which branch that makes the handler
/// take:</para>
/// <list type="bullet">
/// <item><b>Only the I/O leaves are mocked</b>; everything that is pure compute runs for real, the
/// <c>IShoppingCartTotalsCalculator</c> above all — mocking it would measure an almost-empty
/// <c>RecalculateAsync</c>.</item>
/// <item><b>Idempotent without <c>[IterationSetup]</c></b>: the never-cache and the fresh-cart-per-call
/// <c>GetAsync</c> mock (mechanism in <see cref="CartBenchmarkHost"/>) make every invocation load and
/// recalculate its own cart, so a mutation never accumulates and <c>InvocationCount</c> stays free.</item>
/// <item><b>Two axes</b>: <c>LineItemCount</c> (1/5/20/100 — 100 is what surfaces super-linear growth)
/// and <see cref="CartShape"/> (Flat vs Configured). A benchmark that pins one axis says why.</item>
/// </list>
/// </summary>
public abstract class CartBenchmarkBase
{
    /// <summary>The module setup baked by the concrete runner subclass (upstream / a consumer).</summary>
    protected abstract ICartBenchmarkSetup CreateSetup();

    /// <summary>
    /// Composes the benchmark DI container for a cart of the given size and shape. The optional
    /// <paramref name="customizeCart"/> seeds per-op cart state (e.g. a pre-existing shipment/payment)
    /// and <paramref name="customizeServices"/> overrides a leaf for the op's scenario (e.g. an
    /// avail-methods mock returning a matching rate, or a working validation-context factory).
    /// </summary>
    protected IServiceProvider BuildProvider(
        int lineItemCount,
        CartShape shape,
        Action<ShoppingCart> customizeCart = null,
        Action<IServiceCollection> customizeServices = null) =>
        CartBenchmarkHost.BuildProvider(CreateSetup(), lineItemCount, shape, customizeCart, customizeServices);
}
