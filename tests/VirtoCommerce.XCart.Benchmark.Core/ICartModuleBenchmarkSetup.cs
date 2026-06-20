using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The module-agnostic seam that lets the same benchmark fixtures run against the un-extended XCart
/// platform or against a consuming module that overrides the cart graph (subclassed models, a heavier
/// aggregate, extra recalculate middleware). A setup answers the two questions that differ per module:
/// which <see cref="AbstractTypeFactory{T}"/> overrides to register, and which concrete
/// <see cref="CartAggregate"/> to build from the shared, mostly-mocked leaves.
/// </summary>
public interface ICartModuleBenchmarkSetup
{
    /// <summary>
    /// Registers the module's <c>AbstractTypeFactory</c> overrides so the factory-built cart-graph
    /// models (<c>ShoppingCart</c>/<c>LineItem</c>/<c>ConfigurationItem</c>/...) resolve to the
    /// module's subtypes. Called once per benchmark process before any fixture builds a cart. The
    /// upstream setup registers nothing — the platform resolves the base types via the factory's
    /// fallback.
    /// </summary>
    void RegisterTypes();

    /// <summary>
    /// Builds the module's concrete <see cref="CartAggregate"/> from the shared leaves in
    /// <paramref name="context"/>, supplying any module-specific dependencies itself (e.g. a real
    /// recalculate pipeline). This is the single point where <c>new CartAggregate(...)</c> vs a
    /// heavier subclass is decided; the fixtures stay module-agnostic.
    /// </summary>
    CartAggregate CreateAggregate(CartAggregateContext context);

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
