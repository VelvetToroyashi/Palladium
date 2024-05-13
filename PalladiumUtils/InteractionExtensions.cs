using System.Runtime.CompilerServices;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;

namespace PalladiumUtils;

/// <summary>
/// Helper class for responding to interactions.
/// </summary>
public static class InteractionExtensions
{
    public static async Task<Result<IMessage>> RespondAsync
    (
        this IDiscordRestInteractionAPI interactions, 
        IInteractionContext context, 
        string? content = null, 
        IReadOnlyList<IEmbed>? embeds = null, 
        IEnumerable<IEnumerable<IMessageComponent>>? components = null, 
        bool ephemeral = false
    )
    {
        var componentsAsActionRows = components
                                     .AsOptional()
                                     .Map
                                     (
                                         c => (IReadOnlyList<IMessageComponent>)c.Select
                                         (
                                            cs => (IMessageComponent)new ActionRowComponent(cs.ToArray())
                                         ).ToArray()
                                     );
        
        return await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            content.AsOptional(),
            embeds: embeds.AsOptional(),
            components: componentsAsActionRows,
            flags: ephemeral ? MessageFlags.Ephemeral : default
        );
    }
    
}