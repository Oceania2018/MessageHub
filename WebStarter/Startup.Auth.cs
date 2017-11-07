using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebStarter.Auth;

namespace WebStarter
{
    public partial class Startup
    {
        /// <summary>
        /// Authorization
        /// https://github.com/blowdart/AspNetAuthorizationWorkshop/tree/core2
        /// https://docs.microsoft.com/en-us/aspnet/core/security/authorization/
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureJwtAuthService(IServiceCollection services)
        {
            var authConfig = Configuration.GetSection("TokenAuthentication");

            var tokenValidationParameters = new TokenValidationParameters
            {
                // The signing key must match!  
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = JwtSecurityKey.Create(authConfig["SecretKey"]),

                // Validate the JWT Issuer (iss) claim  
                ValidateIssuer = true,
                ValidIssuer = authConfig["Issuer"],

                // Validate the JWT Audience (aud) claim  
                ValidateAudience = true,
                ValidAudience = authConfig["Audience"],

                // Validate the token expiry  
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    }
                };
            });

        }
    }
}
