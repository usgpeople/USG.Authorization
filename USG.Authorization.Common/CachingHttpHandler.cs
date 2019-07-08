using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace USG.Authorization
{
    public class CachingHttpHandler : DelegatingHandler
    {
        static bool mayCache(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Get;
        }

        static bool mayCache(HttpResponseMessage response)
        {
            return
                response.IsSuccessStatusCode &&
                response.Headers.CacheControl?.NoStore != true;
        }

        static HttpResponseMessage copyResponse(
            HttpResponseMessage response, byte[] data)
        {
            var copy = new HttpResponseMessage
            {
                ReasonPhrase = response.ReasonPhrase,
                StatusCode = response.StatusCode,
                Version = response.Version
            };

            foreach (var header in response.Headers)
                copy.Headers.Add(header.Key, header.Value);

            if (data != null)
            {
                copy.Content = new ByteArrayContent(data);

                foreach (var header in response.Content.Headers)
                    copy.Content.Headers.Add(header.Key, header.Value);
            }

            return copy;
        }

        IMemoryCache _cache;

        DateTimeOffset getExpiration(HttpResponseHeaders headers)
        {
            var maxAge = headers.CacheControl?.MaxAge;

            if (maxAge == null)
                return DateTime.Now + DefaultCacheDuration;
            else if (headers.Date != null)
                return headers.Date.Value + maxAge.Value;
            else
                return DateTime.Now + maxAge.Value;
        }

        public CachingHttpHandler(HttpMessageHandler inner, IMemoryCache cache)
            : base(inner)
        {
            _cache = cache;
        }

        public TimeSpan DefaultCacheDuration { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!mayCache(request))
                return await base.SendAsync(request, cancellationToken);

            string key = request.RequestUri.AbsoluteUri;

            if (_cache.TryGetValue<HttpResponseMessage>(key, out var cached))
            {
                // Copy the cached entry to protect cached.Content from
                // disposal
                var cachedData = cached.Content == null ? null :
                        await cached.Content.ReadAsByteArrayAsync();
                return copyResponse(cached, cachedData);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (!mayCache(response))
                return response;

            var expiration = getExpiration(response.Headers);
            if (expiration <= DateTime.Now)
                return response;

            var data = response.Content == null ? null :
                    await response.Content.ReadAsByteArrayAsync();
            var copy = copyResponse(response, data);

            _cache.Set(key, copy, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            });

            return copyResponse(copy, data);
        }
    }
}
