using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Caching;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Builds a real <see cref="PlatformMemoryCache"/> backed by an in-memory cache, mirroring the LEO
    /// component-test harness. The real cache is required because <see cref="ICartAggregateRepository"/>
    /// caches aggregates through it.
    /// </summary>
    internal static class MemoryCacheMockUtils
    {
        public static IMemoryCache CreateCache() => CreateCache(new SystemClock());

        public static IMemoryCache CreateCache(ISystemClock clock) =>
            new MemoryCache(new MemoryCacheOptions { Clock = clock });

        public static PlatformMemoryCache GetPlatformMemoryCache()
        {
            var cachingOptions = Options.Create(new CachingOptions
            {
                CacheEnabled = true,
            });

            return new PlatformMemoryCache(CreateCache(), cachingOptions, Mock.Of<ILogger<PlatformMemoryCache>>());
        }
    }
}
