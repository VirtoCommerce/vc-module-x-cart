using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveFromSavedForLaterItems</c> GraphQL mutation
/// (<see cref="MoveFromSavedForLaterItemsCommandHandler.Handle"/>): a thin pass-through to
/// <see cref="VirtoCommerce.XCart.Core.Services.ISavedForLaterListService"/>.
///
/// <b>Design note</b>: same rationale as <see cref="MoveToSavedForLaterItemsBenchmarks"/> — the
/// handler delegates entirely to the service; <c>ISavedForLaterListService</c> is mocked at the
/// leaf to measure MediatR dispatch + handler overhead only. <c>LineItemCount</c> / <c>CartShape</c>
/// are not applicable (no recalc runs). FLAG: full-path benchmark requires the real service
/// over two harness instances.
/// </summary>
[MemoryDiagnoser]
public class MoveFromSavedForLaterItemsBenchmarks
{
    private MoveFromSavedForLaterItemsCommandHandler _handler = null!;
    private readonly MoveFromSavedForLaterItemsCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateMoveFromSavedForLaterCommand();

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateMoveFromSavedForLaterHandler();

    [Benchmark]
    public Task<CartAggregateWithList> MoveFromSavedForLaterItems() => _handler.Handle(_command, CancellationToken.None);
}
