namespace LagoVista.IoT.Web.Common.Utils
{
    using LagoVista.Core.Interfaces;
    using LagoVista.UserAdmin.Interfaces;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;

    public class EntryIntentService : IEntryIntentService
    {
        private readonly ICacheProvider _cache;
        private readonly IHttpContextAccessor _http;

        public EntryIntentService(ICacheProvider cache, IHttpContextAccessor http)
        {
            _cache = cache;
            _http = http;
        }

        public async Task StashAsync(string path, string targetOrgId = null, string source = null)
        {
            if (!IsValidRelativePath(path))
                return; // fail-safe: do nothing

            var ctx = _http.HttpContext;
            if (ctx == null)
                return;

            var cid = GetCorrelationIdFromCookie(ctx);
            if (string.IsNullOrWhiteSpace(cid))
            {
                cid = Guid.NewGuid().ToString("N");
                SetCorrelationIdCookie(ctx, cid);
            }

            var record = new EntryIntentRecord
            {
                Path = path,
                TargetOrgId = targetOrgId,
                Source = source,
                CreatedUtc = DateTime.UtcNow
            };

            var cacheKey = BuildCacheKey(cid);
            await _cache.AddAsync(cacheKey, record, EntryIntentConstants.Ttl);
        }

        public async Task<EntryIntentRecord> ConsumeAsync()
        {
            var ctx = _http.HttpContext;
            if (ctx == null)
                return null;

            var cid = GetCorrelationIdFromCookie(ctx);
            if (string.IsNullOrWhiteSpace(cid))
                return null;

            // Always clear cookie to avoid loops, even if cache miss.
            DeleteCorrelationIdCookie(ctx);

            var cacheKey = BuildCacheKey(cid);

            // Atomic get+delete; returns null if missing/expired (per your cache behavior).
            var record = await _cache.GetAndDeleteAsync<EntryIntentRecord>(cacheKey);
            if (record == null)
                return null;

            if (!IsValidRelativePath(record.Path))
                return null;

            return record;
        }

        private static string BuildCacheKey(string correlationId)
            => EntryIntentConstants.CacheKeyPrefix + correlationId;

        private static bool IsValidRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Enforce same-origin relative route
            if (!path.StartsWith("/", StringComparison.Ordinal))
                return false;

            // cheap extra hardening: block scheme-relative and absolute URLs
            if (path.StartsWith("//", StringComparison.Ordinal))
                return false;

            if (path.IndexOf("://", StringComparison.Ordinal) >= 0)
                return false;

            return true;
        }

        private static string GetCorrelationIdFromCookie(HttpContext ctx)
        {
            string value;
            return ctx.Request.Cookies.TryGetValue(EntryIntentConstants.CookieName, out value)
                ? value
                : null;
        }

        private static void SetCorrelationIdCookie(HttpContext ctx, string correlationId)
        {
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                // SameSite intentionally omitted per your choice
                Path = "/"
            };

            ctx.Response.Cookies.Append(EntryIntentConstants.CookieName, correlationId, opts);
        }

        private static void DeleteCorrelationIdCookie(HttpContext ctx)
        {
            ctx.Response.Cookies.Delete(EntryIntentConstants.CookieName, new CookieOptions { Path = "/" });
        }
    }
}
