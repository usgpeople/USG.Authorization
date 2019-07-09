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

        protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            => await _callback(request);
    }

    [TestClass]
    public class CachingHttpHandlerTest
    {
        [TestMethod]
        public async Task Get()
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
        public async Task GetCachedNoContent()
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

            Assert.AreNotSame(original, response1);
            Assert.AreNotSame(original, response2);

            Assert.IsNull(response1.Content);
            Assert.IsNull(response2.Content);
        }

        [TestMethod]
        public async Task GetNoCache()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            var original = new HttpResponseMessage(HttpStatusCode.OK);
            var backing = new MockHttpMessageHandler(async r => original);
            var client = new HttpClient(
                new CachingHttpHandler(backing, cache));

            var response1 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));
            var response2 = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://example.com"));

            Assert.AreSame(original, response1);
            Assert.AreSame(original, response2);
        }

        [TestMethod]
        public async Task GetNotFound()
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

            Assert.AreSame(original, response1);
            Assert.AreSame(original, response2);
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

            Assert.AreSame(original, response1);
            Assert.AreSame(original, response2);
        }
    }
}
