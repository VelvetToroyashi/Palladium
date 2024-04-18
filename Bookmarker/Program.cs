using Remora.Rest.Core;
using Remora.Results;
using RemoraHTTPInteractions.Extensions;
using RemoraHTTPInteractions.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables()
       .AddJsonFile("config.json", optional: true);

builder.Services.AddHTTPInteractionAPIs();

WebApplication app = builder.Build();

app.MapGet("/interaction", async (HttpContext context, WebhookInteractionHelper interactions, IConfiguration config) =>
{
    var hasHeaders = DiscordHeaders.TryExtractHeaders(context.Request.Headers, out var timestamp, out var signature);
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    if (!hasHeaders || !DiscordHeaders.VerifySignature(body, timestamp!, signature!, config["PUBLIC_KEY"]!))
    {
        return Results.Unauthorized();
    }

    Result<(string, Optional<IReadOnlyDictionary<string, Stream>>)> result = await interactions.HandleInteractionAsync(body);

    if (!result.IsDefined(out (string stringContent, Optional<IReadOnlyDictionary<string, Stream>> files) content))
    {
        // Discord???
        return Results.BadRequest();
    }

    if (content.files is { IsDefined: false })
    {
        context.Response.Headers.ContentType = "application/json";
        return Results.Ok(content.stringContent);
    }

    MultipartFormDataContent payload = new();
        
    payload.Add(new StringContent(content.stringContent), "payload_json");

    for (int i = 0; i < content.files.Value.Count; i++)
    {
        KeyValuePair<string, Stream> file = content.files.Value.ElementAt(i);
        payload.Add(new StreamContent(file.Value), $"file{i}", file.Key);
    }

    return Results.Ok(payload);
});

app.Run();