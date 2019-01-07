using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace USG.Authorization.AspNetCoreSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            // Uncomment after copying whitelist.txt to C:\inetpub\wwwroot
            //app.UseHostedWhitelist("http://localhost/whitelist.txt");

            app.UseStaticWhitelist("whitelist.txt");

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
