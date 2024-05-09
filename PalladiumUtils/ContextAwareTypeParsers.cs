using Remora.Commands.Parsers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Parsers;
using Remora.Results;

namespace PalladiumUtils;

/// <summary>
/// A stop-gap parser that fixes an issue in Remora regarding how multi-type parsers are handled.
///
/// This parser specifically checks for the message in the context 
/// </summary>
/// <param name="context"></param>
/// <param name="fallback"></param>
public class ContextAwareMessageParser
(
    IOperationContext context, 
    MessageParser fallback
) : AbstractTypeParser<IPartialMessage>
{
    public override ValueTask<Result<IPartialMessage>> TryParseAsync(string token, CancellationToken ct)
    {
        // "Context-aware"; attempt to pull from the interaction, if available, and if it hasn't already been parsed
        if (context is not InteractionContext { Interaction.Data: { HasValue: true, Value: { IsT0: true } data } })
        {
            return ((ITypeParser<IPartialMessage>)fallback).TryParseAsync(token, ct);
        }

        var message = data.AsT0.Resolved.Value.Messages.Value.Values.First();

        return message.ID.Value.ToString() != token ? 
            ((ITypeParser<IPartialMessage>)fallback).TryParseAsync(token, ct) 
            : ValueTask.FromResult(Result<IPartialMessage>.FromSuccess(message));

    }
}