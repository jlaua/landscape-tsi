using System.Threading.RateLimiting;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var bootstrapPassword = Environment.GetEnvironmentVariable("LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD");
if (!string.IsNullOrWhiteSpace(bootstrapPassword))
{
    builder.Configuration["BootstrapAdmin:Password"] = bootstrapPassword;
}

builder.Services.AddControllersWithViews();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Identity.Application";
    options.DefaultAuthenticateScheme = "Identity.Application";
    options.DefaultSignInScheme = "Identity.Application";
    options.DefaultChallengeScheme = "Identity.Application";
});
authentication.AddCookie("Identity.Application", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "__Host-LandscapeTsi.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.EventsType = typeof(LandscapeCookieAuthenticationEvents);
});

var oauthEnabled = builder.Configuration.GetValue<bool>("Authentication:OAuth:Enabled");
if (oauthEnabled)
{
    var authority = builder.Configuration["Authentication:OAuth:Authority"];
    var clientId = builder.Configuration["Authentication:OAuth:ClientId"];
    if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(clientId))
    {
        throw new InvalidOperationException("OAuth está habilitado pero Authority o ClientId no están configurados.");
    }

    authentication.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = "Identity.Application";
        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = builder.Configuration["Authentication:OAuth:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var issuer = context.Principal?.FindFirst("iss")?.Value ?? options.Authority;
                var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<IamUsuario>>();
                var audit = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationAuditWriter>();
                var user = !string.IsNullOrWhiteSpace(subject)
                    ? await users.FindByLoginAsync($"OIDC:{issuer}", subject)
                    : null;

                if (user is not { IsActive: true })
                {
                    await audit.WriteAsync("Login", "OAuth", "Failed", subject, context.HttpContext.TraceIdentifier);
                    context.Fail("La identidad corporativa no está vinculada a un usuario activo.");
                    return;
                }

                context.Principal = await context.HttpContext.RequestServices
                    .GetRequiredService<IUserClaimsPrincipalFactory<IamUsuario>>()
                    .CreateAsync(user);
                if (context.Principal.Identity is System.Security.Claims.ClaimsIdentity identity)
                {
                    identity.AddClaim(new System.Security.Claims.Claim(
                        CustomClaimTypes.AuthenticationMethod,
                        "oidc"));
                }
                await audit.WriteAsync("Login", "OAuth", "Succeeded", user.UserName, context.HttpContext.TraceIdentifier);
            },
            OnRemoteFailure = async context =>
            {
                var audit = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationAuditWriter>();
                await audit.WriteAsync("Login", "OAuth", "Failed", null, context.HttpContext.TraceIdentifier);
                context.HandleResponse();
                context.Response.Redirect("/Account/Login");
            }
        };
    });
}

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.AdministratorPermissions.Keys)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.Services.InitializeIdentityInfrastructureAsync();

app.Run();

public partial class Program;
