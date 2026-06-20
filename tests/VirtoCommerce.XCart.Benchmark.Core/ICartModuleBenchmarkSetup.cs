using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The module-agnostic seam that lets the same benchmark fixtures run against the un-extended XCart
/// platform or against a consuming module that overrides the cart graph (subclassed models, a heavier
/// aggregate, extra recalculate middleware). A setup answers the single question that differs per
/// module: which registrations to contribute to the benchmark DI container so the consumer's own
/// handlers, type overrides, and recalculate pipeline are what the benchmarks measure.
/// </summary>
public interface ICartModuleBenchmarkSetup
{
    /// <summary>
    /// Contributes the module's registrations to the benchmark DI container built by
    /// <see cref="CartBenchmarkHost"/>: the concrete <see cref="CartAggregate"/> (base or a subclass),
    /// the recalculate pipeline launcher (mocked upstream, real for a consumer), and any command/type
    /// overrides (<c>OverrideCommandType</c>/<c>UseCommandType().WithCommandHandler()</c>,
    /// <c>AbstractTypeFactory</c> overrides). Called AFTER Core has registered the base XCart handlers
    /// and the shared mocked I/O leaves, so registrations here win by DI last-registration semantics.
    /// </summary>
    void ConfigureServices(IServiceCollection services);
}
