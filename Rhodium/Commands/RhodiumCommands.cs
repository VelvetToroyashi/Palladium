using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using Rhodium.Services;
using IResult = Remora.Results.IResult;

namespace Rhodium.Commands;

[DiscordInstallContext(ApplicationIntegrationType.GuildInstallable, ApplicationIntegrationType.UserInstallable)]
public class RhodiumCommands
(
    IInteractionContext context,
    UnitConversionService conversionService,
    IDiscordRestInteractionAPI interactionAPI
) : CommandGroup
{
    [Command("Convert Units")]
    [CommandType(ApplicationCommandType.Message)]
    public async Task<IResult> ConvertUnitsAsync(IPartialMessage message)
    {
        var conversions = await conversionService.GetConversionsForMessageAsync(message);

        if (!conversions.IsSuccess)
        {
            await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, conversions.Error.Message);
        }

        var conversionStrings = conversions.Entity.Select(x => $"{x.OriginalUnit} -> {x.ConvertedUnit}").ToArray();

        if (conversionStrings.Length is 0)
        {
            await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, "No conversions found! \n-# *Or* the measurements are already in your preferred unit.");
            return Result.Success;
        }

        await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, string.Join("\n", conversionStrings));

        return Result.Success;
    }
}
