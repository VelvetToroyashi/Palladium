using Bookmarker.Commands;
using Bookmarker.Data;
using Bookmarker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PalladiumUtils;
using Remora.Commands.Extensions;
using Remora.Commands.Parsers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Parsers;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Extensions;
using RemoraHTTPInteractions.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables()
       .AddJsonFile("config.json", optional: true);

builder.Services.AddDiscordGateway(s => s.GetService<IConfiguration>()!["CLIENT_TOKEN"]!);
builder.Services.AddHttpInteractions();

builder.Services.AddDiscordCommands(true, false);

builder.Services.AddSingleton<BookmarkService>();
builder.Services.AddDbContextFactory<BookmarkContext>();
builder.Services.AddCommandTree().WithCommandGroup<BookmarkCommands>();

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

builder.Services.Configure<InteractionResponderOptions>(o => o.SuppressAutomaticResponses = true);
WebApplication app = builder.Build();

app.AddInteractionEndpoint();

await app.Services.GetRequiredService<SlashService>().UpdateSlashCommandsAsync();
var db = app.Services.GetRequiredService<IDbContextFactory<BookmarkContext>>().CreateDbContext().Database;

await db.MigrateAsync();
db.EnsureCreated();

app.Run();

