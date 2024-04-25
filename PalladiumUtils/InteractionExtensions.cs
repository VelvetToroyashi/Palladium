using System.Runtime.CompilerServices;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;

namespace PalladiumUtils;

/// <summary>
/// Helper class for responding to interactions.
/// </summary>
public static class InteractionExtensions
{
    public static async Task<Result<IMessage>> RespondAsync(this IDiscordRestInteractionAPI interactions, IInteractionContext context, string content, IReadOnlyList<IEmbed>? embeds = null, bool ephemeral = false)
    {
        return await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            content,
            embeds: embeds.AsOptional(),
            flags: (MessageFlags)(64 * Unsafe.BitCast<bool, int>(ephemeral))
        );
    }
    
}