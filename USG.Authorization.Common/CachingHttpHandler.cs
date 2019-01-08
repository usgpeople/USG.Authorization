using System;
using System.Net.Http;
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
                response.Headers.CacheControl != null &&
                response.Headers.CacheControl.NoStore == false &&
                response.Headers.CacheControl.MaxAge != null;
        }

        static HttpResponseMessage copyResponse(
            HttpResponseMessage response, byte[] data)
        {
            var copy = new HttpResponseMessage
            {
                Content = new ByteArrayContent(data),
                ReasonPhrase = response.ReasonPhrase,
                StatusCode = response.StatusCode,
                Version = response.Version
            };

            foreach (var header in response.Headers)
                copy.Headers.Add(header.Key, header.Value);
            foreach (var header in response.Content.Headers)
                copy.Content.Headers.Add(header.Key, header.Value);

            return copy;
        }

        IMemoryCache _cache;

        public CachingHttpHandler(HttpMessageHandler inner, IMemoryCache cache)
            : base(inner)
        {
            _cache = cache;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!mayCache(request))
                return await base.SendAsync(request, cancellationToken);

            string key = request.RequestUri.AbsoluteUri;

            if (_cache.TryGetValue<HttpResponseMessage>(key, out var cached))
                return cached; 

            var response = await base.SendAsync(request, cancellationToken);

            if (!mayCache(response))
                return response;

            var data = await response.Content.ReadAsByteArrayAsync();
            var copy = copyResponse(response, data);

            return _cache.Set(key, copy, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    (response.Headers.Date ?? DateTime.Now) +
                    response.Headers.CacheControl.MaxAge,
                Size = data.Length
            });
        }
    }
}
