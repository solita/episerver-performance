using System;
using System.Web.Caching;
using EPiServer;

namespace Solita.Episerver.Performance.Helpers
{
    internal static class CacheUtil
    {
        public static T GetOrStoreUsingDatafactoryDependency<T>(Cache cache, string key, double expireSeconds, Func<T> generator)
        {
            var dependency = new CacheDependency(null, new[] { DataFactoryCache.VersionKey });
            var container = GetOrStoreContainer<T>(cache, key, expireSeconds, dependency);

            if (!container.HasValue)
            {
                lock (container)
                {
                    if (!container.HasValue)
                    {
                        container.Value = generator();
                        container.HasValue = true;
                    }
                }
            }

            return container.Value;
        }

        private static Container<T> GetOrStoreContainer<T>(Cache cache, string key, double expireSeconds, CacheDependency dependency)
        {
            var container = cache[key];

            if (container == null)
            {
                lock (cache)
                {
                    container = cache[key];
                    if (container == null)
                    {
                        container = new Container<T>();
                        cache.Add(key, container, dependency, DateTime.Now.AddSeconds(expireSeconds), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    }
                }
            }

            return (Container<T>)container;
        }

        private class Container<T>
        {
            public T Value { get; set; }
            public bool HasValue { get; set; }
        }
    }
}