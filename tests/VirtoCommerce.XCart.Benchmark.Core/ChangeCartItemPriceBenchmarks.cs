using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemPrice</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: the mutate-existing-cart path — load (real build + recalc), apply a manual
/// price to the first line item, save (recalc again). Only the I/O leaves are mocked; the totals
/// calculator is real. Idempotent without [IterationSetup] (the cart service returns a fresh cart per
/// call). Flat vs Configured surfaces configured-product regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public abstract class ChangeCartItemPriceBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ChangeCartItemPriceCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartBenchmarkFixtures.CreateChangeCartItemPriceCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemPrice() => _mediator.Send(_command);
}
