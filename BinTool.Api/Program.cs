using BinTool.Api.Extensions;
using BinTool.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.AddApplicationMiddleware();

app.Run();
