using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;

namespace USG.Authorization
{
    public static class WhitelistMiddleware
    {
        public static void UseWhitelist(
            this IApplicationBuilder app,
            Func<Task<IWhitelist>> whitelistProvider)
        {
            app.Use(async (context, next) =>
            {
                var whitelist = await whitelistProvider();
                var ip = context.Connection.RemoteIpAddress;

                if (whitelist.Match(ip))
                {
                    await next();
                }
                else
                {
                    var message = Encoding.ASCII.GetBytes(
                        $"Host {ip} is not whitelisted for this site.");

                    context.Response.StatusCode = 403;
                    await context.Response.Body.WriteAsync(message);
                }
            });
        }

        public static void UseStaticWhitelist(
            this IApplicationBuilder app,
            string path)
        {
            // Shared for all requests
            var whitelist = Whitelist.Parse(File.ReadAllText(path));

            app.UseWhitelist(() => Task.FromResult<IWhitelist>(whitelist));
        }

        public static void UseHostedWhitelist(
            this IApplicationBuilder app,
            string url,
            HttpClient client = null)
        {
            // Shared HTTP client for all requests
            if (client == null)
            {
                // Use IMemoryCache from DI if available, e.g. if configured
                // with services.AddMemoryCache()
                var cache = (MemoryCache)app.ApplicationServices
                        .GetService(typeof(IMemoryCache))
                    ?? new MemoryCache(new MemoryCacheOptions());

                // Default to 5 minutes caching if not forbidden by NoStore
                // or specified by Cache-Control headers.
                var handler = new CachingHttpHandler(
                        new HttpClientHandler(), cache);
                handler.DefaultCacheDuration = new TimeSpan(0, 5, 0);

                client = new HttpClient(handler);
            }

            // GetStringAsync is called on every IP check, but HttpClient
            // will honour caching headers
            app.UseWhitelist(async () =>
                Whitelist.Parse(await client.GetStringAsync(url)));
        }
    }
}
