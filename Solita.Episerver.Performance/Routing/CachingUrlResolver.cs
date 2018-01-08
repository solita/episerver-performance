using System;
using System.Linq;
using System.Web;
using System.Web.Routing;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Globalization;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Internal;
using EPiServer.Web.Routing.Segments.Internal;

namespace Solita.Episerver.Performance.Routing
{
    /// <summary>
    /// Caches UrlResolver.GetVirtualPath(ContentReference, string, VirtualPathArguments) results in ContextMode.Default (end-user view).
    /// Cache invalidates when Episerver content changes.
    /// </summary>
    public class CachingUrlResolver : DefaultUrlResolver
    {
        private const int CacheTimeSeconds = 3600;
        private static IObjectInstanceCache _cache;
        private readonly IContentCacheVersion _cacheVersion;
        private readonly string _cacheVersionKey;

        public CachingUrlResolver(RouteCollection routes,
                                  IContentLoader contentLoader,
                                  ISiteDefinitionRepository siteDefinitionRepository,
                                  TemplateResolver templateResolver,
                                  IPermanentLinkMapper permanentLinkMapper,
                                  IContentLanguageSettingsHandler contentLanguageSettingsHandler,
                                  IContentCacheKeyCreator cacheKeyCreator,
                                  IContentCacheVersion cacheVersion,
                                  IObjectInstanceCache cache)
            : base(routes, contentLoader, siteDefinitionRepository, templateResolver, permanentLinkMapper, contentLanguageSettingsHandler)
        {
            _cache = cache;
            _cacheVersion = cacheVersion;
            _cacheVersionKey = cacheKeyCreator.VersionKey;
        }

        public override VirtualPathData GetVirtualPath(ContentReference contentLink, string language, VirtualPathArguments args)
        {
            if (IgnoreCache(contentLink, args))
            {
                return base.GetVirtualPath(contentLink, language, args);
            }

            var cachekey = CreateCacheKey(contentLink, language, args);
            var value = _cache.Get(cachekey) as VirtualPathData;

            if (value == null)
            {
                value = base.GetVirtualPath(contentLink, language, args);

                if (value != null)
                {
                    _cache.Insert(cachekey, value, CreateCacheEvictionPolicy());
                }
            }

            return value;
        }

        private bool IgnoreCache(ContentReference contentLink, VirtualPathArguments args)
        {
            // "DataFactoryCache.Version" key must exists in the cache. Otherwise the entries are not cached. 
            // The key is removed when a remote server content is updated. 
            // Version call ensures that the key is present
            var version = _cacheVersion.Version;

            // Cache only in Default context mode, i.e. only for end users, not for edit or preview mode
            return HttpContext.Current == null || contentLink == null || !IsDefaultContextActive(args);
        }

        private static bool IsDefaultContextActive(VirtualPathArguments args)
        {
            // VirtualPathArguments.ContextMode can contain incorrect values, so also
            // check if RequestSegmentContext.CurrentContextMode is Default. 
            // This handles all the valid use cases in edit and preview modes.
            return (RequestSegmentContext.CurrentContextMode == ContextMode.Default) &&
                   (args == null || args.ContextMode == ContextMode.Default || args.ContextMode == ContextMode.Undefined);
        }

        private CacheEvictionPolicy CreateCacheEvictionPolicy()
        {
            return new CacheEvictionPolicy(TimeSpan.FromSeconds(CacheTimeSeconds), CacheTimeoutType.Absolute, new[] { _cacheVersionKey });
        }

        private static string CreateCacheKey(ContentReference contentLink, string language, VirtualPathArguments args)
        {
            // Default to PreferredCulture when explicit value is empty
            var activeLanguage = !string.IsNullOrEmpty(language) ? language : ContentLanguage.PreferredCulture.Name;
            // URLs are startpage relative
            var startPageLink = ContentReference.StartPage ?? PageReference.EmptyReference;

            var key = "Solita:CachingUrlResolver.GetVirtualPath" + $"/{activeLanguage}/{startPageLink}/{contentLink}";

            // RouteValues contain segments for partial routers. These must be included in the key
            if (args?.RouteValues != null && args.RouteValues.Any())
            {
                key += "/" + string.Join(",", args.RouteValues.Select(p => p.Key + "=" + p.Value));
            }

            return key;
        }
    }
}