using qa_portal_apis.application.interfaces;
using qa_portal_apis.application.users;
using qa_portal_apis.infrastructure;
using qa_portal_apis.infrastructure.auth;


DotNetEnv.Env.Load();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

Console.WriteLine("\n=== Configuration Audit ===");
foreach (var c in builder.Configuration.AsEnumerable())
{
    if (c.Key.Contains("AzureAd", StringComparison.OrdinalIgnoreCase) || c.Key.Contains("Frontend", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"{c.Key} = {(c.Key.Contains("Secret") ? "******" : c.Value)}");
    }
}
Console.WriteLine("==========================\n");

builder.Services.AddControllers();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddScoped<GetOrCreateUser>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();

builder.Services.AddBffAuthentication(builder.Configuration);

builder.Services.AddCors(o =>
{
    o.AddPolicy("frontend", p => p
        .WithOrigins(
            (builder.Configuration["FrontendUrl"] ?? "https://localhost:5173").TrimEnd('/')
        )
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"[HTTP] {context.Request.Method} {context.Request.Path}");
    await next();
});

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
});

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();