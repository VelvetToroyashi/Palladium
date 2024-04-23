using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Remora.Rest.Core;
using Remora.Results;
using RemoraHTTPInteractions.Services;

namespace PalladiumUtils;

/// <summary>
/// An extension class to de-duplicate adding interaction endpoint handling in projects.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Adds a `/interaction` endpoint to handle HTTP interactions from Discord.
    /// </summary>
    /// <param name="app">The web application to add the endpoint to</param>
    /// <returns>The route handler builder.</returns>
    /// <remarks>This method assumes that a public key will be available in the
    /// configuration of the app with the key of "PUBLIC_KEY".</remarks>
    public static RouteHandlerBuilder AddInteractionEndpoint(this WebApplication app)
    {
        return app.MapGet("/interaction", async (HttpContext context, WebhookInteractionHelper interactions, IConfiguration config) =>
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

            if (content.files.IsDefined())
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
    }
}