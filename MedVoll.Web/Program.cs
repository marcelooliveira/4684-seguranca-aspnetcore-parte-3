using MedVoll.Web.Filters;
using MedVoll.Web.Interfaces;
using MedVoll.Web.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ExceptionHandlerFilter>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ExceptionHandlerFilter>();
});


builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddRazorPages();

builder.Services.AddTransient<IMedVollApiService, MedVollApiService>();

var httpClientName = builder.Configuration["MedVoll.WebApi.Name"];
var httpClientUrl = builder.Configuration["MedVoll.WebApi.Url"];

builder.Services.AddHttpClient(
    httpClientName,
    client =>
    {
        // Configura o endereço-base do cliente nomeado.
        client.BaseAddress = new Uri(httpClientUrl);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5)) // Recria o handler a cada 5 minutos
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 10;
        options.Retry.OnRetry = args =>
        {
            var exception = args.Outcome.Exception!;
            Console.WriteLine($"Falha na chamada à API! Tentando novamente em 5 segundos. Msg: {exception.Message}");
            return default;
        };
        options.Retry.Delay = TimeSpan.FromSeconds(5); // Intervalo entre tentativas
    });

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "VollMedAuthCookie";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["MedVoll.Identity.Url"];
        options.ClientId = "MedVoll.Web";
        options.ClientSecret = "secret";
        options.ResponseType = "code";

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("MedVoll.WebAPI");

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;
    });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/erro/500");
    app.UseStatusCodePagesWithReExecute("/erro/{0}");
}

app.UseStaticFiles();


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
