using System;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Base for every cart benchmark. The benchmark <b>logic</b> ([Benchmark] methods, [Params]) lives in
/// per-operation abstract subclasses of this type in the Core library; each runner exe defines a
/// concrete subclass (source-generated) overriding <see cref="CreateSetup"/> to bake its module setup.
/// BenchmarkDotNet discovers the concrete subclass in the runner assembly and runs the inherited
/// [Benchmark] methods out-of-process — the runner's own <c>.csproj</c> is rebuilt for the child, so
/// the baked setup is active there with no process-global state and no custom toolchain.
///
/// <para><see cref="BuildProvider"/> composes the DI container (base XCart handlers + mocked I/O leaves
/// + the module's <see cref="ICartModuleBenchmarkSetup.ConfigureServices"/> overrides); operations
/// resolve <c>IMediator</c> (command/query benchmarks) or <c>Func&lt;CartAggregate&gt;</c>
/// (aggregate-direct benchmarks) from it.</para>
/// </summary>
public abstract class CartBenchmarkBase
{
    /// <summary>The module setup baked by the concrete runner subclass (upstream / a consumer).</summary>
    protected abstract ICartModuleBenchmarkSetup CreateSetup();

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
