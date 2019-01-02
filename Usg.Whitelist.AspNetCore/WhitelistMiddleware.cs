using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Usg.Whitelist
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
            var whitelist = WhitelistParser.Parse(File.ReadAllText(path));

            app.UseWhitelist(() => Task.FromResult(whitelist));
        }

        public static void UseHostedWhitelist(
            this IApplicationBuilder app,
            string url)
        {
            var client = new HttpClient();

            app.UseWhitelist(async () =>
                WhitelistParser.Parse(await client.GetStringAsync(url)));
        }
    }
}
