using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace USG.Authorization.Tests
{
    class MockHttpMessageHandler : HttpMessageHandler
    {
        Func<HttpRequestMessage, Task<HttpResponseMessage>> _callback;

        public MockHttpMessageHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
        {
            _callback = callback;
        }

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return await _callback(request);
        }
    }

    [TestClass]
    public class CachingHttpHandlerTest
    {
        [TestMethod]
        public async Task Get_Cached()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            original.ReasonPhrase = "Pretty good!";
            original.Headers.Date = new DateTime(2028, 12, 24);
            original.Headers.CacheControl = new CacheControlHeaderValue();
            original.Headers.CacheControl.MaxAge = new TimeSpan(0, 5, 0);
            original.Content = new StringContent("Hello, World!");
            original.Content.Headers.ContentLanguage.Add("en");

            var backing = new MockHttpMessageHandler(async r => original);

            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(1, backing.CallCount); // cached

            Assert.AreNotSame(original, response1);
            Assert.AreNotSame(original, response2);
            Assert.AreNotSame(response1, response2);
            Assert.AreNotSame(original.Content, response1.Content);
            Assert.AreNotSame(original.Content, response2.Content);
            Assert.AreNotSame(response1.Content, response2.Content);

            Assert.AreEqual(original.ReasonPhrase, response1.ReasonPhrase);
            Assert.AreEqual(original.Headers.Date, response1.Headers.Date);
            Assert.AreEqual(
                original.Headers.CacheControl.MaxAge,
                response1.Headers.CacheControl.MaxAge);
            Assert.AreEqual(
                original.Content.Headers.ContentLanguage.First(),
                response1.Content.Headers.ContentLanguage.FirstOrDefault());
            Assert.AreEqual(
                await original.Content.ReadAsStringAsync(),
                await response1.Content.ReadAsStringAsync());

            // Don't know how to obtain cache entry to see if the expiration
            // is correct.
        }

        [TestMethod]
        public async Task Get_CachedNoContent()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            original.Headers.Date = new DateTime(2028, 12, 24);
            original.Headers.CacheControl = new CacheControlHeaderValue();
            original.Headers.CacheControl.MaxAge = new TimeSpan(0, 5, 0);

            var backing = new MockHttpMessageHandler(async r => original);
            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(1, backing.CallCount); // cached

            Assert.IsNull(response1.Content);
            Assert.IsNull(response2.Content);
        }

        [TestMethod]
        public async Task Get_NoCacheControl()
        {
            // No cache control headers, so the request may be cached, but no
            // DefaultCacheDuration so it shouldn't be.

            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            var backing = new MockHttpMessageHandler(async r => original);
            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(2, backing.CallCount); // not cached
        }

        [TestMethod]
        public async Task Get_DefaultCacheDuration()
        {
            // No cache control headers, so the request may be cached, and with
            // DefaultCacheDuration so it should be.

            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            original.Content = new StringContent("Hello, World!");

            var backing = new MockHttpMessageHandler(async r => original);

            var handler = new CachingHttpHandler(backing, cache);
            handler.DefaultCacheDuration = new TimeSpan(0, 5, 0);

            var client = new HttpClient(handler);

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(1, backing.CallCount); // cached
        }

        [TestMethod]
        public async Task Get_NoStore()
        {
            // DefaultCacheDuration set but caching forbidden with NoStore so
            // should not be cached.

            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            original.Headers.CacheControl = new CacheControlHeaderValue();
            original.Headers.CacheControl.NoStore = true;

            var backing = new MockHttpMessageHandler(async r => original);

            var handler = new CachingHttpHandler(backing, cache);
            handler.DefaultCacheDuration = new TimeSpan(0, 5, 0);

            var client = new HttpClient(handler);

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(2, backing.CallCount); // not cached
        }

        [TestMethod]
        public async Task Get_NotFound()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var original = new HttpResponseMessage(HttpStatusCode.NotFound);
            var backing = new MockHttpMessageHandler(async r => original);
            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreEqual(2, backing.CallCount); // not cached
        }

        [TestMethod]
        public async Task Post()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var original = new HttpResponseMessage(HttpStatusCode.OK);
            var backing = new MockHttpMessageHandler(async r => original);
            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "http://example.com"));

            Assert.AreEqual(2, backing.CallCount); // not cached
        }
    }
}
