using BlazorApp2.Data;
using BlazorApp2.Models.Configurations;
using BlazorApp2.Services;
using BlazorApp2.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BlazorApp2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            GraphClientConfiguration graphClientConfiguration = new GraphClientConfiguration();
            Configuration.Bind("AzureAD", graphClientConfiguration);
            
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        var roleGroups = new Dictionary<string, string>();
                        Configuration.Bind("AuthorizationGroups", roleGroups);

                        var graphService = await GraphService.CreateOnBehalfOfUserAsync(ctx.SecurityToken.RawData, Configuration);
                        var memberGroups = await graphService.CheckMemberGroupsAsync(roleGroups.Keys);

                        var claims = memberGroups.Select(groupGuid => new Claim(ClaimTypes.Role, roleGroups[groupGuid])).ToList();
                        claims.Add(new Claim("token", ctx.SecurityToken.RawData));

                        var appIdentity = new ClaimsIdentity(claims);
                        ctx.Principal.AddIdentity(appIdentity);
                    }
                };
            });

            services.Configure<CookieAuthenticationOptions>(AzureADDefaults.CookieScheme, options => options.AccessDeniedPath = "/accessdenied");


            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
            services.AddSingleton<IGraphService2>(instance =>
            {
                return new GraphService2(graphClientConfiguration);
            });
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");

                //endpoints.MapGet("/", async context =>
                //{
                //    await context.Response.WriteAsync("Im authenticated (no role required).");
                //}).RequireAuthorization();

                endpoints.MapGet("/roletest", async context =>
                {
                    await context.Response.WriteAsync("You passed the role test!");
                }).RequireAuthorization(new AuthorizeAttribute() { Roles = "examplerole1" });

                endpoints.MapGet("/accessdenied", async context =>
                {
                    await context.Response.WriteAsync("Access denied!");
                });
            });

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //    endpoints.MapBlazorHub();
            //    endpoints.MapFallbackToPage("/_Host");
            //});
        }
    }
}
