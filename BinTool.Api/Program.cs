using BinTool.Api.Extensions;
using BinTool.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddIdentityServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddWebApi()
    .AddSwaggerDocumentation()
    .AddApplicationServices();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.UseApplicationPipeline();

app.Run();
