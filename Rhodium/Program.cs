using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PalladiumUtils;
using Remora.Commands.Extensions;
using Remora.Commands.Parsers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Parsers;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using RemoraHTTPInteractions.Extensions;
using Rhodium.Commands;
using Rhodium.Data;
using Rhodium.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Configuration
       .AddEnvironmentVariables()
       .AddJsonFile("config.json", optional: true);

builder.Services.AddDiscordGateway(s => s.GetService<IConfiguration>()!["CLIENT_TOKEN"]!);
builder.Services.AddHttpInteractions();

builder.Services
       .AddDiscordCommands(true, false)
       .AddCommandTree()
       .WithCommandGroup<RhodiumCommands>();

builder.Services.AddScoped<ITypeParser<IMessage>, MessageParser>();
builder.Services.Replace
(
       ServiceDescriptor.Describe
       (
              typeof(ITypeParser<IPartialMessage>),
              typeof(ContextAwareMessageParser),
              ServiceLifetime.Scoped
       )
);

builder.Services.AddDbContextFactory<RhodiumContext>();
builder.Services.AddTransient<UnitConversionService>();

var app = builder.Build();

await app.Services.GetRequiredService<SlashService>().UpdateSlashCommandsAsync();
var db = app.Services.GetRequiredService<IDbContextFactory<RhodiumContext>>().CreateDbContext().Database;

await db.MigrateAsync();

// Configure the HTTP request pipeline.

app.AddInteractionEndpoint();

await app.RunAsync();
