USG.Authorization
=================
Currently contains ASP.NET and ASP.NET Core middleware that compares the
client IP against a local whitelist file or one hosted on a webserver,
returning a `403 Forbidden` response if there is no match.

Setup (ASP.NET Core)
--------------------
After adding a USG.Authorization.AspNetCore package reference, modify Startup.cs
as follows:

    using USG.Authorization;

    // ...

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        // ...

        // To use a local whitelist file, relative to the project root
        // directory. See below for syntax.
        app.UseStaticWhitelist("whitelist.txt");

        // To use a hosted file.
        app.UseHostedWhitelist("http://localhost/whitelist.txt");

        // To use a custom whitelist provider
        app.UseWhitelist(async () => /* return an IHash<IPAddress> */)

        // ...
    }

Any middleware (`Use...()`) added after the whitelisting middleware is subject
to the whitelisting so take care to place the calls appropariately.

Setup (ASP.NET)
---------------
After installing the USG.Authorization.AspNet package, add one of the HTTP modules
in Web.config:

    <system.webServer>
      <modules>
        <!-- To use a local whitelist file, configured through the
             whitelist:Path app setting. See below for syntax. -->
        <add name="HostedWhitelist"
             type="USG.Authorization.HostedWhitelistModule,USG.Authorization.AspNet"/>

        <!-- To use a remote whitelist file, configured through the
             whitelist:Url app setting. -->
        <add name="StaticWhitelist"
             type="USG.Authorization.StaticWhitelistModule,USG.Authorization.AspNet"/>
      </modules>
    </system.webServer>

Any HTTP module added after the chosen whitelisting module is subject to the
whitelisting so take care to order the modules appropriately.

Configure the chosen module in the `appSettings` section:

    <appSettings>
      <add key="whitelist:Path" value="~/whitelist.txt"/>
      <add key="whitelist:Url" value="http://localhost/whitelist.txt"/>
    </appSettings>

Note that a local path should be rooted with `~`.

Whitelist syntax
----------------
One IPv4 or IPv6 address per line. Comments starting with `#` and empty lines
are ignored.

Example:

    127.0.0.1  # Allow local access
    ::1        # Allow local access over IPv6
    8.8.8.8    # Allow Google DNS

Behaviour
---------
Any exceptions retrieving the whitelist are bubled up, most likely yielding an
"Internal Server Error" response.

Static whitelists are read once at first use and not updated.

Hosted whitelists are fetched for every request with a single `HttpClient`
instance which will cache responses as allowed by the response headers. It
is advisable to configure the server hosting the whitelist appropriately.

Custom whitelist providers are called for every request and are responsible
for any desired caching behaviour.

Customisation
-------------
**Reponse**: use custom error pages or middelware inserted prior to the
whitelisting middleware to customise the 403 response.

**Caching**: configure the server hosting the whitelist to return standard
HTTP response headers to control if and how the downloaded whitelists are
cached.

**Granularity**: in the ASP.NET Core version, the middleware can be added
only to certain routes or prefixes  through ASP.NET Core's default mechanism.

**Environments**: use Web.config transforms, app.config transforms or code to
switch between different whitelists depending on the environment.

**Provider**: inject a custom provider with `app.UseWhitelist(async() => ...)`
to use an altogether different kind of whitelist source. (`WhitelistParser`
from `USG.Authorization.Common` can be used for parsing the whitelist format
documented above.
