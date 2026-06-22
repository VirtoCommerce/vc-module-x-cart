using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// The module-agnostic seam that lets the same benchmark fixtures run against the un-extended XCart
/// platform or against a consuming module that overrides the cart graph (subclassed models, a heavier
/// aggregate, extra recalculate middleware). A setup answers the single question that differs per
/// module: which registrations to contribute to the benchmark DI container so the consumer's own
/// handlers, type overrides, and recalculate pipeline are what the benchmarks measure.
/// </summary>
public interface ICartBenchmarkSetup
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

    /// <summary>
    /// Optional hook letting a consumer supply its own loaded-cart graph for the loaded-cart benchmark
    /// paths (mutation / recalculate / validate / checklist). Returning <c>null</c> (the default) uses
    /// Core's generic <see cref="CartBenchmarkFixtures.CreateCart"/> shape; a consumer overrides it to
    /// feed its domain graph (e.g. a parent→child line-item hierarchy) so its recalculate pipeline and
    /// validators do real per-item work instead of early-returning on the generic shape.
    ///
    /// <para><b>Id contract</b>: a consumer-supplied cart MUST expose selected line items with ids
    /// <c>li-0..li-{lineItemCount-1}</c> (and product ids <c>product-0..</c>), because the shared
    /// mutation command fixtures target <c>li-0</c> by id. A cart that omits those ids makes the
    /// id-targeted mutation benchmarks silently early-return — measuring nothing — so any richer graph
    /// must still place valid mutation targets at those ids.</para>
    ///
    /// <para>Only the loaded-cart path is affected; the add path's products still come from the host's
    /// product-loader mock. <paramref name="shape"/> carries the same Flat/Configured intent as Core's
    /// default — a consumer is free to interpret it in its own domain terms.</para>
    /// </summary>
    ShoppingCart CreateCart(int lineItemCount, CartShape shape) => null;
}
