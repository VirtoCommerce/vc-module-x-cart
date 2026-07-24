using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCartItemsBulk</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the storefront quick-order / CSV path that addresses items by SKU. The
/// handler resolves the SKUs via the indexed search (mocked per-scenario through
/// <c>customizeServices</c>), dedups, then delegates to the real <c>addCartItems</c> handler (real add
/// dispatch + recalc). The SKU resolution itself is I/O (mocked); what this measures on top of the plain
/// <c>addCartItems</c> benchmark is the bulk dedup/match envelope plus the bulk→singular delegation.
///
/// Two axes: <b>Shape</b> (<c>Configured</c> routes the inner add through the configured-product
/// dispatch); <b>Item count</b> (1 = single, 5/20/100 = bulk; 100 surfaces super-linear growth). Read
/// the <c>Allocated</c> column across the rows.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class AddCartItemsBulkBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddCartItemsBulkCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int ItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(
                ItemCount,
                Shape,
                customizeServices: services => CartBenchmarkFixtures.AddBulkProductSearchMock(services, ItemCount))
            .GetRequiredService<IMediator>();
        // After BuildProvider so a consumer's OverrideCommandType registration is in effect.
        _command = CartBenchmarkFixtures.CreateAddCartItemsBulkCommand(ItemCount);
    }

    [Benchmark]
    public Task<BulkCartResult> AddCartItemsBulk() => _mediator.Send(_command);
}
