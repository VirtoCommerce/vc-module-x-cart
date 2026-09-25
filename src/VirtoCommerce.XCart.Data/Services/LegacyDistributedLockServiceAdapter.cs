using System;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Xapi.Core.Infrastructure;
using IDistributedLockService = VirtoCommerce.Xapi.Core.Infrastructure.IDistributedLockService;

namespace VirtoCommerce.XCart.Data.Services;

/// <summary>
/// Lets the obsolete constructors that take the XAPI <see cref="IDistributedLockService"/> keep working now that
/// the builders and <c>PurchaseSchema</c> lock through <see cref="IDistributedLock"/>.
/// The legacy service keeps its own wait and error: <c>timeout</c> and <c>cancellationToken</c> are ignored,
/// and a busy resource surfaces its <see cref="LockError"/>.
/// </summary>
[Obsolete("Bridges the obsolete XAPI IDistributedLockService. Pass IDistributedLock from VirtoCommerce.Platform.Core.DistributedLock instead.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
internal sealed class LegacyDistributedLockServiceAdapter(IDistributedLockService distributedLockService) : IDistributedLock
{
    public async Task<IDistributedLockHandle> AcquireAsync(string resource, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var acquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // The legacy service only runs a callback under the lock, so the callback stays open until the handle is disposed.
        var execution = distributedLockService.ExecuteAsync(resource, async () =>
        {
            acquired.TrySetResult();
            await released.Task.ConfigureAwait(false);
            return true;
        });

        if (await Task.WhenAny(acquired.Task, execution).ConfigureAwait(false) == execution)
        {
            // The lock was not taken: rethrow the legacy error (LockError when the resource stays busy).
            await execution.ConfigureAwait(false);
        }

        return new Handle(resource, released, execution);
    }

    public async Task<IDistributedLockHandle> TryAcquireAsync(string resource, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        try
        {
            return await AcquireAsync(resource, timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (LockError)
        {
            return null;
        }
    }

    private sealed class Handle(string resource, TaskCompletionSource released, Task execution) : IDistributedLockHandle
    {
        public string Resource { get; } = resource;

        public void Dispose()
        {
            released.TrySetResult();
        }

        public async ValueTask DisposeAsync()
        {
            released.TrySetResult();
            await execution.ConfigureAwait(false);
        }
    }
}
