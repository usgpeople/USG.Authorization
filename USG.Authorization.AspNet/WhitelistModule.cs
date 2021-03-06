﻿using Microsoft.Extensions.Caching.Memory;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace USG.Authorization
{
    // These classes aren't tested because it's nigh impossible to mock
    // HttpApplication and friends:
    //
    //  - Properties are read only so cannot be assigned with test values.
    //  - Classes and members are nonvirtual so they cannot be mocked with
    //    Moq.
    //  - Extracting these modules to service classes would require a bunch
    //    of extra classes and interfaces and then the IHttpModules still
    //    wouldn't be tested as such.

    public class StaticWhitelistModule : IHttpModule
    {
        Whitelist _whitelist;

        void beginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;

            if (_whitelist == null)
            {
                // Have to do this here becase MapPath() isn't allowed in
                // Init().
                string path = application.Server.MapPath(
                    ConfigurationManager.AppSettings["whitelist:Path"]);
                string data = File.ReadAllText(path);

                _whitelist = Whitelist.Parse(data);
            }

            var request = application.Context.Request;
            var ip = IPAddress.Parse(request.UserHostAddress);

            if (!_whitelist.Match(ip))
            {
                var response = application.Context.Response;
                response.StatusCode = 403;
                response.Output.WriteLine($"Host {ip} is not whitelisted " +
                    $"for this site.");
                response.End();
            }
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += beginRequest;
        }

        public void Dispose() { }
    }

    public class HostedWhitelistModule : IHttpModule
    {
        HttpClient _client;
        string _url;

        async Task beginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var request = application.Context.Request;
            var ip = IPAddress.Parse(request.UserHostAddress);

            var data = await _client.GetStringAsync(_url);
            var whitelist = Whitelist.Parse(data);

            if (!whitelist.Match(ip))
            {
                var response = application.Context.Response;
                response.StatusCode = 403;
                response.Output.WriteLine($"Host {ip} is not whitelisted " +
                    $"for this site.");
                response.End();
            };
        }

        public void Init(HttpApplication context)
        {
            var handler = new CachingHttpHandler(
                new HttpClientHandler(),
                new MemoryCache(new MemoryCacheOptions()));

            if (TimeSpan.TryParse(
                    ConfigurationManager.AppSettings["whitelist:DefaultCacheDuration"],
                    out var duration))
                handler.DefaultCacheDuration = duration;

            _client = new HttpClient(handler);
            _url = ConfigurationManager.AppSettings["whitelist:Url"];

            var eventHandler = new EventHandlerTaskAsyncHelper(beginRequest);

            context.AddOnBeginRequestAsync(
                eventHandler.BeginEventHandler,
                eventHandler.EndEventHandler);
        }

        public void Dispose() { }
    }
}
