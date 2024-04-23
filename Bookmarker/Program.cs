using PalladiumUtils;
using RemoraHTTPInteractions.Extensions;



WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables()
       .AddJsonFile("config.json", optional: true);

builder.Services.AddHTTPInteractionAPIs();

WebApplication app = builder.Build();

app.AddInteractionEndpoint();

app.Run();