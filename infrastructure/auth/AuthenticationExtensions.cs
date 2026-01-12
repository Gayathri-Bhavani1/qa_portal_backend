using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using qa_portal_apis.application.interfaces;

namespace qa_portal_apis.infrastructure.auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Debug: Print configuration values
        var instance = configuration["AzureAd:Instance"];
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];
        
        Console.WriteLine($"Instance: {instance}");
        Console.WriteLine($"TenantId: {tenantId}");
        Console.WriteLine($"ClientId: {clientId}");
        Console.WriteLine($"ClientSecret: {(string.IsNullOrEmpty(clientSecret) ? "MISSING" : "SET")}");
        
        if (string.IsNullOrEmpty(instance))
        {
            throw new InvalidOperationException("AzureAd:Instance is not configured");
        }
        
        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("AzureAd:ClientId is not configured");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(
                microsoftIdentityOptions =>
                {
                    // Manually bind configuration
                    microsoftIdentityOptions.Instance = configuration["AzureAd:Instance"]!;
                    microsoftIdentityOptions.TenantId = configuration["AzureAd:TenantId"] ?? "common";
                    microsoftIdentityOptions.ClientId = configuration["AzureAd:ClientId"]!;
                    microsoftIdentityOptions.ClientSecret = configuration["AzureAd:ClientSecret"];
                    microsoftIdentityOptions.CallbackPath = configuration["AzureAd:CallbackPath"] ?? "/signin-oidc";
                },
                cookieOptions =>
                {
                    cookieOptions.Cookie.Name = "QAPortal.Auth";
                    cookieOptions.Cookie.HttpOnly = true;
                    cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    cookieOptions.Cookie.SameSite = SameSiteMode.Lax;  // Lax for same-site (localhost to localhost)
                    cookieOptions.Cookie.Domain = "localhost";  // Explicitly set domain
                    cookieOptions.Cookie.Path = "/";
                    cookieOptions.Cookie.IsEssential = true;
                    cookieOptions.SlidingExpiration = true;
                    cookieOptions.ExpireTimeSpan = TimeSpan.FromHours(8);

                    cookieOptions.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/me"))
                            {
                                ctx.Response.StatusCode = 401;
                            }
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = ctx =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api"))
                            {
                                ctx.Response.StatusCode = 403;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

        // Configure OpenIdConnect options AFTER AddMicrosoftIdentityWebApp
        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                NameClaimType = "name",
                RoleClaimType = "roles"
            };
            
            // OIDC cookies - These go through Azure AD so need special handling
            options.CorrelationCookie.SameSite = SameSiteMode.None;  // None because it goes through Azure AD
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Domain = "localhost";
            
            options.NonceCookie.SameSite = SameSiteMode.None;  // None because it goes through Azure AD
            options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.NonceCookie.HttpOnly = true;
            options.NonceCookie.Domain = "localhost";

            // SAFELY HOOK EVENTS (Delegate complex logic to OidcEventHandlers)
            var previousOnTicketReceived = options.Events.OnTicketReceived;
            options.Events.OnTicketReceived = async context =>
            {
                // Run previous handlers first (if any)
                await previousOnTicketReceived(context);
                await OidcEventHandlers.HandleTicketReceived(context);
            };

            var previousOnRedirectToIdentityProvider = options.Events.OnRedirectToIdentityProvider;
            options.Events.OnRedirectToIdentityProvider = async context =>
            {
                // Check for API requests first
                await OidcEventHandlers.HandleApiUnauthenticated(context);
                
                // If handled (200 OK returned), stop processing
                if (context.Handled) return;

                await previousOnRedirectToIdentityProvider(context);
            };

            var previousOnRedirectToIdentityProviderForSignOut = options.Events.OnRedirectToIdentityProviderForSignOut;
            options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
            {
                 await previousOnRedirectToIdentityProviderForSignOut(context);
                 await OidcEventHandlers.ConfigureLogoutRequest(context);
            };

            options.Events.OnSignedOutCallbackRedirect = OidcEventHandlers.HandleSignedOutCallback;

            var previousOnAuthenticationFailed = options.Events.OnAuthenticationFailed;
            options.Events.OnAuthenticationFailed = async context =>
            {
                await previousOnAuthenticationFailed(context);
                await OidcEventHandlers.HandleAuthenticationFailed(context);
            };

            var previousOnRemoteFailure = options.Events.OnRemoteFailure;
            options.Events.OnRemoteFailure = async context =>
            {
                await previousOnRemoteFailure(context);
                await OidcEventHandlers.HandleRemoteFailure(context);
            };
        });

        services.AddAuthorization();
        return services;
    }
}
