using System.Text;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Identity.Presentation;
using Alcedo.Framework.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Alcedo.Framework.Identity.ProgramExtensions;

public static partial class ProgramExtensions
{
    public static IServiceCollection AddAlcedoIdentityAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(nameof(JwtOptions))
            .Get<JwtOptions>() ?? throw new InvalidOperationException("Missing JwtOptions in appsettings.json");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AlcedoIdentityScheme.AuthenticationScheme;
                options.DefaultChallengeScheme = AlcedoIdentityScheme.AuthenticationScheme;
            })
            .AddJwtBearer(AlcedoIdentityScheme.AuthenticationScheme,options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,
                    AuthenticationType = AlcedoIdentityScheme.AuthenticationScheme,
                    
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtOptions.Issuer,

                    ValidAudience = jwtOptions.Audience,

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        if (context.AuthenticateFailure is SecurityTokenExpiredException)
                        {
                            throw new IdentityErrJwtExpired();
                        }

                        throw new IdentityErrUnauthenticated();
                    }
                };
            });
        return services;
    }

    public static IServiceCollection AddAlcedoIdentityAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AlcedoRbacHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AlcedoAuthorizePolicy.Rbac, policy =>
            {
                policy.AddAuthenticationSchemes(AlcedoIdentityScheme.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new AlcedoPermissionRequirement());
            });
        });

        return services;
    }
}
