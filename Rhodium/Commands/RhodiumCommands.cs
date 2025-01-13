using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using Rhodium.Data;
using Rhodium.Services;
using IResult = Remora.Results.IResult;

namespace Rhodium.Commands;

[Ephemeral]
[AllowedContexts(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
[DiscordInstallContext(ApplicationIntegrationType.GuildInstallable, ApplicationIntegrationType.UserInstallable)]
public class RhodiumCommands
(
    IInteractionContext context,
    UnitConversionService conversionService,
    IDiscordRestInteractionAPI interactionAPI,
    IDbContextFactory<RhodiumContext> dbContextFactory
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

            return Result.Success;
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

    [Command("configure")]
    [Description("Set your preferred units for temperature and measurement.")]
    public async Task<IResult> ConfigureAsync(RhodiumMeasurement preferredMeasurement, RhodiumTemperature preferredTemperature)
    {
        await using RhodiumContext dbContext = await dbContextFactory.CreateDbContextAsync();

        var userID = context.Interaction.User.OrDefault(context.Interaction.Member.Value.User.Value).ID;
        var userConfig = await dbContext.UserConfigs.FindAsync(userID);

        if (userConfig is null)
        {
            userConfig = new RhodiumUserConfig
            {
                ID = userID,
                PreferredMeasurementUnit = preferredMeasurement,
                PreferredTemperatureUnit = preferredTemperature
            };

            await dbContext.UserConfigs.AddAsync(userConfig);
        }
        else
        {
            userConfig.PreferredMeasurementUnit = preferredMeasurement;
            userConfig.PreferredTemperatureUnit = preferredTemperature;
        }

        await dbContext.SaveChangesAsync();

        await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, "Configuration saved!");

        return Result.Success;
    }

    [Command("purge_preferences")]
    [Description("Remove your preferred units for temperature and measurement.")]
    public async Task<IResult> PurgePreferencesAsync()
    {
        await using RhodiumContext dbContext = await dbContextFactory.CreateDbContextAsync();

        var userID = context.Interaction.User.OrDefault(context.Interaction.Member.Value.User.Value).ID;
        var userConfig = await dbContext.UserConfigs.FindAsync(userID);

        if (userConfig is not null)
        {
            dbContext.UserConfigs.Remove(userConfig);
            await dbContext.SaveChangesAsync();
        }

        await interactionAPI.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, "Preferences purged!");

        return Result.Success;
    }
}
