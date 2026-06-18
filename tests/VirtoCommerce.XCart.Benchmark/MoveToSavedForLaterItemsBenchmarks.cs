using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveToSavedForLaterItems</c> GraphQL mutation
/// (<see cref="MoveToSavedForLaterItemsCommandHandler.Handle"/>): a thin pass-through to
/// <see cref="VirtoCommerce.XCart.Core.Services.ISavedForLaterListService"/>.
///
/// <b>Design note</b>: the handler delegates entirely to the service, which internally does multiple
/// I/O operations (load source cart, search/create the saved-for-later list, save both). Mocking
/// <c>ISavedForLaterListService</c> at the leaf measures the MediatR dispatch + handler overhead
/// only — consistent with the cluster's "everything I/O is mocked at the leaf" rule. Since the
/// handler never touches a repository or the cart aggregate directly, <c>LineItemCount</c> and
/// <c>CartShape</c> axes are not sensible (no recalc path runs). The benchmark captures the
/// pure-handler dispatch baseline.
///
/// FLAG: to benchmark the full path (list-search + double-save), wire the real
/// <see cref="VirtoCommerce.XCart.Data.Services.SavedForLaterListService"/> over two
/// <see cref="CartBenchmarkFixtures.MutationHarness"/> instances (source cart + saved-for-later
/// list). That is a separate fixture out of scope for this baseline cluster.
/// </summary>
[MemoryDiagnoser]
public class MoveToSavedForLaterItemsBenchmarks
{
    private MoveToSavedForLaterItemsCommandHandler _handler = null!;
    private readonly MoveToSavedForLaterItemsCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateMoveToSavedForLaterCommand();

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateMoveToSavedForLaterHandler();

    [Benchmark]
    public Task<CartAggregateWithList> MoveToSavedForLaterItems() => _handler.Handle(_command, CancellationToken.None);
}
