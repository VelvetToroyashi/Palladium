using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;

namespace PalladiumUtils;

/// <summary>
/// A stop-gap parser that fixes an issue in Remora regarding how multi-type parsers are handled. <br/>
///
/// This parser specifically checks for the message in the context 
/// </summary>
/// <param name="context"></param>
/// <param name="fallback"></param>
public class ContextAwareMessageParser
(
    IOperationContext context, 
    ITypeParser<IMessage> fallback
) : AbstractTypeParser<IPartialMessage>
{
    public async override ValueTask<Result<IPartialMessage>> TryParseAsync(string token, CancellationToken ct)
    {
        if (!DiscordSnowflake.TryParse(token, out Snowflake? mid))
        {
            return new ParsingError<IPartialMessage>("Invalid snowflake");
        }
        
        Snowflake messageID = mid.Value; 
        
        // "Context-aware"; attempt to pull from the interaction, if available, and if it hasn't already been parsed
        if 
        (
            context is InteractionContext
            {
                Interaction.Data: { HasValue: true, Value.Value: IApplicationCommandData data }
            } &&
            data.Resolved.Value.Messages.Value.TryGetValue(messageID, out IPartialMessage? message)
        )
        {
            return Result<IPartialMessage>.FromSuccess(message);
        }

        Result<IMessage> fallbackResult = await fallback.TryParseAsync(token, ct);
        return fallbackResult.Map(parsed => (IPartialMessage) parsed);
    }
}