using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace USG.Authorization
{
    public static class WhitelistMiddleware
    {
        public static void UseWhitelist(
            this IApplicationBuilder app,
            Func<Task<ISet<IPAddress>>> whitelistProvider)
        {
            app.Use(async (context, next) =>
            {
                var whitelist = await whitelistProvider();

                if (whitelist.Contains(context.Connection.RemoteIpAddress))
                    await next();
                else
                    context.Response.StatusCode = 403;
            });
        }

        public static void UseStaticWhitelist(
            this IApplicationBuilder app,
            string path)
        {
            // Shared for all requests
            var whitelist = WhitelistParser.Parse(File.ReadAllText(path));

            app.UseWhitelist(() => Task.FromResult(whitelist));
        }

        public static void UseHostedWhitelist(
            this IApplicationBuilder app,
            string url)
        {
            // Shared for all requests
            var client = new HttpClient();

            // GetStringAsync is called on every IP check, but HttpClient
            // will honour caching headers
            app.UseWhitelist(async () =>
                WhitelistParser.Parse(await client.GetStringAsync(url)));
        }
    }
}
