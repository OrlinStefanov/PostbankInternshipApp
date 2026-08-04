using BinTool.UI.Components;
using BinTool.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The UI keeps the API's bearer token inside its own authentication cookie: encrypted
// by Data Protection, HttpOnly so no script can read it, and Secure so it never travels
// over plain HTTP. That is what makes it safer than local storage.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BinTool.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";

        // The cookie's own lifetime is set per sign-in to match the token's expiry, so
        // the session cannot outlive the token it carries.
        options.SlidingExpiration = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();

// Typed clients for the BinTool API. Base URL is configurable; the dev cert is
// accepted only in Development so localhost HTTPS calls don't fail the handshake.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7258";

builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(ApiHandler);

builder.Services.AddHttpClient<BinImportApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(ApiHandler);

builder.Services.AddHttpClient<BinRangeApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(ApiHandler);

HttpClientHandler ApiHandler()
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Authentication must run before antiforgery. An antiforgery token is bound to the user
// it was generated for, so validating it before HttpContext.User is populated compares it
// against an anonymous caller - which breaks every form posted by a signed-in user.
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
