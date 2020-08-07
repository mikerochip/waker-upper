using Amazon.CloudWatchEvents;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace WakerUpper.Asp
{
    public class Startup
    {
        #region Constants
        private const string AspKeysEnvVar = "AspKeysParameter";
        #endregion

        #region Properties
        private static string AspKeysParameterName => Environment.GetEnvironmentVariable(AspKeysEnvVar);
        
        private IConfiguration Configuration { get; }
        #endregion

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddRazorPages();

            services.AddDataProtection()
                .PersistKeysToAWSSystemsManager(AspKeysParameterName);

            services.AddLogging(builder => builder
                .ClearProviders()
                .AddLambdaLogger(new LambdaLoggerOptions
                {
                    IncludeException = true,
                    IncludeNewline = true,
                })
            );
            // got the idea for this from https://stackoverflow.com/a/57590076
            // this forces the ASP DI system to instantiate ILogger<AppLogger> when someone wants
            // to DI a plain ILogger, which we're doing because you can't instantiate a plain ILogger
            services.AddSingleton<ILogger>(provider => provider.GetService<ILogger<AppLogger>>());
            
            services.AddAWSService<IAmazonCloudWatchEvents>();
            services.AddAWSService<IAmazonSimpleSystemsManagement>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseCookiePolicy();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            //app.UseAuthentication();
            //app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
