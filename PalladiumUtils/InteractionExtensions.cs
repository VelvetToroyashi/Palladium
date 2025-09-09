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
        bool ephemeral = false,
        bool isComponentsV2 = false
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

        MessageFlags flags = default;

        if (isComponentsV2)
        {
            flags |= MessageFlags.IsComponentsV2;
        }

        if (ephemeral)
        {
            flags |= MessageFlags.Ephemeral;
        }
        
        return await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            content.AsOptional(),
            embeds: embeds.AsOptional(),
            components: componentsAsActionRows,
            flags: flags
        );
    }
    
    public static async Task<Result<IMessage>> RespondComponentsV2Async
    (
        this IDiscordRestInteractionAPI interactions, 
        IInteractionContext context, 
        string? content = null, 
        IReadOnlyList<IEmbed>? embeds = null, 
        IEnumerable<IMessageComponent>? components = null, 
        bool ephemeral = false,
        bool isComponentsV2 = false
    )
    {

        MessageFlags flags = default;

        if (isComponentsV2)
        {
            flags |= MessageFlags.IsComponentsV2;

            if (!string.IsNullOrEmpty(content) && components is null)
            {
                components = [new TextDisplayComponent(content)];
                content = null;
            }
            
        }

        if (ephemeral)
        {
            flags |= MessageFlags.Ephemeral;
        }
        
        return await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            content.AsOptional(),
            embeds: embeds.AsOptional(),
            allowedMentions: new AllowedMentions((MentionType[])[]),
            components: components?.ToArray() ?? default(Optional<IReadOnlyList<IMessageComponent>>),
            flags: flags
        );
    }
}