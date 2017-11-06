using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MessageService;
using Microsoft.EntityFrameworkCore;

namespace WebStarter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());

            app.Use(async (context, next) =>
            {
                if (string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"]))
                {
                    if (context.Request.QueryString.HasValue)
                    {
                        // for JWT authentication
                        var token = context.Request.QueryString.Value
                            .Split('&')
                            .SingleOrDefault(x => x.Contains("authorization"))?.Split('=')[1];
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            context.Request.Headers.Add("Authorization", new[] { $"Bearer {token}" });
                        }

                        // Get user id from QueryString
                        var userId = context.Request.QueryString.Value
                            .Split('&')
                            .SingleOrDefault(x => x.Contains("userId"))?.Split('=')[1];

                        var claim = new System.Security.Claims.Claim("userId", userId);
                        var claims = new List<System.Security.Claims.Claim> { claim };
                        context.User.AddIdentity(new System.Security.Claims.ClaimsIdentity(claims));
                    }
                }
                await next.Invoke();
            });

            app.UseSignalR(config => {
                config.MapHub<MessageHub>(String.Empty);
                MessageHubInitializer.Init((user) => {
                    user.Name = "haiping";
                    user.FirstName = "Haiping";
                    user.LastName = "Chen";
                });
            });

            app.UseMvc();
        }
    }
}
