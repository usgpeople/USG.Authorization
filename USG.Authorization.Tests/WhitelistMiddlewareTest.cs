using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace USG.Authorization.Tests
{
    [TestClass]
    public class WhitelistMiddlewareTest
    {
        static void spoofIp(HttpContext context)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("::1");
        }

        static TestServer createWhitelistHost(
            Func<string> contentCallback,
            bool allowCaching = false)
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Map("/whitelist.txt", app2 =>
                {
                    app.Use(async (context, next) =>
                    {
                        await context.Response.Body.WriteAsync(
                            Encoding.UTF8.GetBytes(contentCallback()));
                    });
                });
            });

            return new TestServer(builder);
        }

        [TestMethod]
        public async Task UseWhitelist_Match()
        {
            var whitelist = Whitelist.Parse("::1");

            var builder = new WebHostBuilder().Configure(app =>
            {
                app.UseWhitelist(async () => whitelist);
            });

            using (var server = new TestServer(builder))
            {
                var context = await server.SendAsync(spoofIp);

                Assert.AreEqual(404, context.Response.StatusCode); ;
            }
        }

        [TestMethod]
        public async Task UseWhitelist_NoMatch()
        {
            var whitelist = Whitelist.Parse("");

            var builder = new WebHostBuilder().Configure(app =>
            {
                app.UseWhitelist(async () => whitelist);
            });

            using (var server = new TestServer(builder))
            {
                var context = await server.SendAsync(spoofIp);

                Assert.AreEqual(403, context.Response.StatusCode);
            }
        }

        [TestMethod]
        public async Task UseWhitelist_Repeated()
        {
            int count = 0;

            var whitelist = Whitelist.Parse("");

            var builder = new WebHostBuilder().Configure(app =>
            {
                app.UseWhitelist(async () =>
                {
                    count++;
                    return whitelist;
                });
            });

            using (var server = new TestServer(builder))
            {
                await server.SendAsync(spoofIp);
                await server.SendAsync(spoofIp);
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task UseStaticWhitelist_Match()
        {
            string filename = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(filename, "::1");

                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseStaticWhitelist(filename);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(404, context.Response.StatusCode);
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [TestMethod]
        public async Task UseStaticWhitelist_NoMatch()
        {
            string filename = Path.GetTempFileName();

            try
            {
                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseStaticWhitelist(filename);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(403, context.Response.StatusCode);
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [TestMethod]
        public async Task UseStaticWhitelist_Repeated()
        {
            string filename = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(filename, "::1");

                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseStaticWhitelist(filename);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(404, context.Response.StatusCode);

                    await File.WriteAllTextAsync(filename, "");
                    context = await server.SendAsync(spoofIp);

                    // Should not have reread the file
                    Assert.AreEqual(404, context.Response.StatusCode);
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [TestMethod]
        public async Task UseHostedWhitelist_Match()
        {
            string whitelist = "::1";

            using (var whitelistServer = createWhitelistHost(() => whitelist))
            using (var whitelistClient = whitelistServer.CreateClient())
            {
                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseHostedWhitelist(
                        "http://example.com/whitelist.txt", whitelistClient);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(404, context.Response.StatusCode);
                }
            }
        }

        [TestMethod]
        public async Task UseHostedWhitelist_NoMatch()
        {
            string whitelist = "";

            using (var whitelistServer = createWhitelistHost(() => whitelist))
            using (var whitelistClient = whitelistServer.CreateClient())
            {
                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseHostedWhitelist(
                        "http://example.com/whitelist.txt", whitelistClient);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(403, context.Response.StatusCode);
                }
            }
        }

        [TestMethod]
        public async Task UseHostedWhitelist_Multiple()
        {
            string whitelist = "::1";

            using (var whitelistServer = createWhitelistHost(() => whitelist))
            using (var whitelistClient = whitelistServer.CreateClient())
            {
                var builder = new WebHostBuilder().Configure(app =>
                {
                    app.UseHostedWhitelist(
                        "http://example.com/whitelist.txt", whitelistClient);
                });

                using (var server = new TestServer(builder))
                {
                    var context = await server.SendAsync(spoofIp);

                    Assert.AreEqual(404, context.Response.StatusCode);

                    whitelist = "";
                    context = await server.SendAsync(spoofIp);

                    // Should re-request resource. It would be nice to test
                    // that HttpClient does indeed cache responses but our
                    // whitelistClient is a mocked thing that doesn't.
                    Assert.AreEqual(403, context.Response.StatusCode);
                }
            }
        }
    }
}
