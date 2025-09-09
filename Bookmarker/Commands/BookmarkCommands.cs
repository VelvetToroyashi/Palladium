using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using Bookmarker.Data;
using Bookmarker.Services;
using JetBrains.Annotations;
using PalladiumUtils;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Interactivity;
using Remora.Discord.Interactivity.Services;
using Remora.Rest.Core;
using Remora.Results;
//using RemoraHTTPInteractions.Services;

namespace Bookmarker.Commands;

/// <summary>
/// Commands for managing bookmarks.
/// </summary>
[Ephemeral]
[PublicAPI]
[AllowedContexts(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
[DiscordInstallContext(ApplicationIntegrationType.UserInstallable)]
public class BookmarkCommands
(
    IInteractionContext context,
    IDiscordRestInteractionAPI interactions,
    BookmarkService bookmarks,
    SlashService slashService
) : CommandGroup
{
    private const string NoBookmarksMessage = """
                                              You don't have any bookmarks! 

                                              Try /help or Apps ➜ Bookmark This!
                                              """;

    private const string NoBookmarksWithTagMessage = """
                                                     You don't have any bookmarks with the tag {0}!

                                                     Use Apps ➜ Bookmark This (Advanced)! to create bookmarks with tags!
                                                     """;

    public const string BookmarkFormat = """
                                         ### **`{0}`** | <@{1}> in <#{2}>:
                                         > {3}
                                         -# This bookmark is tagged as: {5}
                                         -# This bookmark has {4} attachment(s).

                                         """;

    public const int MaxBookmarksPerPage = 5;
    

    [Command("Bookmark This!")]
    [CommandType(ApplicationCommandType.Message)]
    public async Task<Result> CreateQuickBookmarkAsync(IPartialMessage message)
    {
        Snowflake? guildID = context.Interaction.GuildID.AsNullable();
        Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

        Result<BookmarkEntity> bookmark = await bookmarks.CreateBookmarkAsync(userID, guildID, [], message);

        string content = bookmark.IsSuccess
        ? "Bookmark created!"
        : "Failed to create bookmark! \n" +
          "If this issue persists, please forward the following to `@velvet.toroyashi`: \n" +
          $"```{bookmark.Error.Message}```";

        return (Result)await interactions.RespondAsync(context, content, ephemeral: true);
    }

    [Command("Bookmark This! (Advanced)")]
    [CommandType(ApplicationCommandType.Message)]
    public async Task<Result> CreateBookmarkAdvancedAsync(IPartialMessage message)
    {
        Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

        if (await bookmarks.HasMessagedBookmarkedAsync(message.ID.Value, userID))
        {
            return (Result)await interactions.RespondAsync(context, "You've already bookmarked this message!", ephemeral: true);
        }

        string state = RandomNumberGenerator.GetHexString(16, true);
        InMemoryDataService<string, IPartialMessage>.Instance.TryAddData(state, message);

        InteractionModalCallbackData callbackData = new
        (
            CustomIDHelpers.CreateModalIDWithState("create_bookmark", state),
            "Create a bookmark",
            [
                new ActionRowComponent
                (
                    [
                        new TextInputComponent
                        (
                            "tags",
                            TextInputStyle.Short,
                            "Tags",
                            3,
                            100,
                            true,
                            default,
                            "Tags for the bookmark, seperated by commas"
                        )
                    ]
                ),
            ]
        );

        return await interactions.CreateInteractionResponseAsync
        (
            context.Interaction.ID,
            context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.Modal,
                new(callbackData)
            )
        );
    }

    [Ephemeral]
    [Command("bookmark_list")]
    [Description("Get a list of your bookmarks.")]
    public async Task<Result> GetBookmarksAsync([AutocompleteProvider("list_autocomplete_tags")] [Description("Filter by bookmarks with this tag")] string? tag = null)
    {
        Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

        IReadOnlyList<BookmarkEntity> userBookmarks = await bookmarks.GetBookmarksAsync(userID);

        if (userBookmarks.Count is 0)
        {
            return (Result)await interactions.RespondAsync(context, NoBookmarksMessage, ephemeral: true);
        }
      
        IReadOnlyList<IMessageComponent> components = CreateComponents
        (
            1,
            userBookmarks,
            tag,
            MaxBookmarksPerPage
        );

        var res = await interactions.RespondComponentsV2Async(context, components: components, ephemeral: true, isComponentsV2: true);

        return (Result)res;
    }


    [Command("invite")]
    [Description("Get the invite link for the bot.")]
    public async Task<Result> GetInviteLinkAsync()
        => (Result)await interactions.RespondAsync(context, $"https://discord.com/application-directory/{context.Interaction.ApplicationID}", ephemeral: true);

    [Command("help")]
    [Description("Shows a tutorial on how to use the bot.")]
    public async Task<Result> GetHelpAsync()
    {
        Snowflake bookmarkListCommandID = slashService.CommandMap.First(c => c.Value.AsT1.Key == "bookmark_list").Key.CommandID;
        Snowflake inviteCommandID = slashService.CommandMap.First(c => c.Value.AsT1.Key == "invite").Key.CommandID;

        const string githubLink = "https://github.com/VelvetToroyashi/Palladium/issues/new";
        const string supportServerLink = "https://discord.gg/MMw2aXuHSQ";

        var content =
        $"""
         First and foremost, thanks for using Bookmarker! 
         Here's a quick guide on how to use the bot:

         Bookmarking is done by right-clicking (or on mobile, long-pressing) 
         and then selecting Apps ➜ Bookmark This! or Apps ➜ Bookmark This! (Advanced).

         The basic command will bookmark the message you clicked on, while the advanced command
         will prompt you for a *tag* to add to the bookmark. Tags are useful for organizing your bookmarks.

         You can view your bookmarks by using </bookmark_list:{bookmarkListCommandID}>!

         If you'd like to share the bot with your friends, you can use the </invite:{inviteCommandID}> command.
         Alternatively, pass this invite link: <https://bookmarker.toroyashi.me/invite>

         If you've encountered a bug with the bot, or have any suggestions, please [open an issue on GitHub](<{githubLink}>).
         Alternatively, join the [support server](<{supportServerLink}>).
         """;

        ButtonComponent[] links =
        [
            new ButtonComponent(ButtonComponentStyle.Link, "Open an Issue", URL: githubLink),
            new ButtonComponent(ButtonComponentStyle.Link, "Join the Support Server", URL: supportServerLink)
        ];

        return (Result)await interactions.RespondAsync(context, content, components: [links], ephemeral: true);
    }

    internal static IReadOnlyList<IMessageComponent> GetBookmarkComponents
    (
        BookmarkEntity bookmarkEntity,
        bool hasBeenDisplayed = false,
        bool canUseDisplayButton = true
    )
    {
        string bookmarkTagString = bookmarkEntity.Tags.Length > 0 ? string.Join(", ", bookmarkEntity.Tags.Select(t => $"`{t}`")) : "None";

        string bookmarkInformation = $"""
                                      ## Viewing Bookmark `{bookmarkEntity.ID}`
                                      You bookmarked this message <t:{bookmarkEntity.CreatedAt.ToUnixTimeSeconds()}:R>.
                                      This bookmark is from <@{bookmarkEntity.AuthorID}> in <#{bookmarkEntity.ChannelID}>.
                                      -# This bookmark has the following tags: {bookmarkTagString}.
                                      """;

        var textBody = new TextDisplayComponent(bookmarkInformation);
        var bookmarkContent = new TextDisplayComponent("# Bookmark Content:\n" + bookmarkEntity.Content);
        var attachments = bookmarkEntity.Attachments.Select(a => new MediaGalleryItem(new UnfurledMediaItem(a))).ToArray();
        
        var bookmarkButtons = new ActionRowComponent
        (
            [
                new ButtonComponent
                (
                    ButtonComponentStyle.Primary, "Display bookmark", IsDisabled: !canUseDisplayButton || hasBeenDisplayed,
                    CustomID: CustomIDHelpers.CreateButtonIDWithState("display_bookmark", bookmarkEntity.ID)
                ),
                new ButtonComponent
                (
                    ButtonComponentStyle.Danger, "Delete bookmark",
                    CustomID: CustomIDHelpers.CreateButtonIDWithState("delete_bookmark", bookmarkEntity.ID)
                ),
                new ButtonComponent
                (
                    ButtonComponentStyle.Link, "Original bookmark",
                    URL:
                    $"https://discord.com/channels/{bookmarkEntity.GuildID?.ToString() ?? "@me"}/{bookmarkEntity.ChannelID}/{bookmarkEntity.MessageID}"
                ),
            ]
        );

        var divider = new SeparatorComponent(IsDivider: true, Spacing: SeparatorSpacingSize.Large);
        List<IMessageComponent> components = [textBody, bookmarkButtons, divider, bookmarkContent];

        if (attachments.Length > 0)
        {
            components.Add(new MediaGalleryComponent(attachments));
        }

        return [new ContainerComponent(components, AccentColour: Color.LightSkyBlue)];
    }
    
    internal static IReadOnlyList<IMessageComponent> CreateComponents
    (
        int page,
        IReadOnlyList<BookmarkEntity> userBookmarks,
        string? tag,
        int maxBookmarksPerPage
    )
    {
        if (!string.IsNullOrEmpty(tag))
        {
            userBookmarks = userBookmarks.Where(b => b.Tags.Contains(tag)).ToArray();
        }
        
        int potentialPages = userBookmarks.Count / maxBookmarksPerPage + Math.Min(1, userBookmarks.Count % maxBookmarksPerPage);
        BookmarkEntity[] bookmarkSlice = userBookmarks.Skip(MaxBookmarksPerPage * (page - 1)).Take(MaxBookmarksPerPage).ToArray();
        SectionComponent[] bookmarkSections = new SectionComponent[bookmarkSlice.Length];
      
        Debug.Assert(bookmarkSlice.Length > 0);
        for (int i = 0; i < bookmarkSlice.Length; i++)
        {
            BookmarkEntity bookmark = bookmarkSlice[i];
            string bookmarkTags = bookmark.Tags.Any() ? string.Join(", ", bookmark.Tags.Select(t => $"`{t}`")) : "None";
            
            var content = string.Format
            (
                BookmarkFormat,
                bookmark.ID,
                bookmark.AuthorID,
                bookmark.ChannelID,
                bookmark.PartialContent,
                bookmark.Attachments.Length,
                bookmarkTags
            );
        
            bookmarkSections[i] = new SectionComponent
            (
                [new TextDisplayComponent(content)],
                new ButtonComponent
                (
                    ButtonComponentStyle.Primary, 
                    Label: "View", 
                    CustomID: CustomIDHelpers.CreateButtonIDWithState("show_bookmark", bookmark.ID)
                )
            );
        }

        ContainerComponent container = new(bookmarkSections, AccentColour: Color.LightSkyBlue);

        return potentialPages <= 1 ? [container] : [container, new ActionRowComponent(GetNavigationButtons())];

        IButtonComponent[] GetNavigationButtons()
        {
            bool leftPageDisabled = page == 1;
            bool rightPageDisabled = page == potentialPages;
        
            Debug.Assert(!(leftPageDisabled && rightPageDisabled));

            return
            [
                new ButtonComponent(ButtonComponentStyle.Secondary, "Last Page", IsDisabled: leftPageDisabled, CustomID: CustomIDHelpers.CreateButtonIDWithState("backward", $"{Math.Min(1, page - 1)}:{tag}")),
                new ButtonComponent(ButtonComponentStyle.Success,   "Next Page", IsDisabled: rightPageDisabled, CustomID: CustomIDHelpers.CreateButtonIDWithState("forward",  $"{page + 1}:{tag}")),
            ];
        }
    }

    internal static void CreateEmbedAndSelectComponent
    (
        int page,
        IReadOnlyList<BookmarkEntity> bookmarks,
        string? tag,
        out IEmbed embed,
        out ISelectMenuComponent selectMenu
    )
    {
        StringBuilder sb = new();
        BookmarkEntity[] bookmarkSlice = bookmarks.Skip(MaxBookmarksPerPage * (page - 1)).Take(MaxBookmarksPerPage).ToArray();

        Debug.Assert(bookmarkSlice.Length > 0);
        ISelectOption[] options = new ISelectOption[bookmarkSlice.Length];

        for (var i = 0; i < bookmarkSlice.Length; i++)
        {
            BookmarkEntity bookmark = bookmarkSlice[i];

            var content = string.Format
            (
                BookmarkFormat,
                bookmark.ID,
                bookmark.AuthorID,
                bookmark.ChannelID,
                bookmark.PartialContent,
                bookmark.Attachments.Length
            );

            sb.AppendLine(content);

            options[i] = new SelectOption($"View Bookmark {bookmark.ID}", bookmark.ID);
        }

        embed = new Embed
        {
            Title = tag is null ? "Your bookmarks" : $"Your bookmarks with the tag ``{tag}``",
            Description = sb.ToString(),
            Colour = tag is null ? Color.PaleGreen : Color.LightBlue,
            Footer = new EmbedFooter($"Page {page} of {bookmarks.Count / MaxBookmarksPerPage + 1}"),
        };

        selectMenu = new StringSelectComponent
        (
            CustomIDHelpers.CreateSelectMenuID("show_bookmark"),
            options,
            "Select a bookmark",
            MaxValues: 1
        );
    }
}

[Ephemeral]
public class BookmarkComponentHandler(IDiscordRestInteractionAPI interactions, IInteractionContext context, BookmarkService bookmarks) : InteractionGroup
{
    [Button("show_bookmark")]
    public async Task<Result> ViewBookmarkAsync(string state)
    {
        _ = context.TryGetUserID(out Snowflake userID);

        Result<BookmarkEntity> bookmarkResult = await bookmarks.GetBookmarkAsync(state, userID);

        if (!bookmarkResult.IsDefined(out BookmarkEntity? bookmark))
        {
            return (Result)await interactions.RespondAsync(context, $"Failed to retrieve bookmark! \n {bookmarkResult.Error}", ephemeral: true);
        }
        
        var canUseDisplayButton = context
                              .Interaction
                              .Member
                              .FlatMap(m => m.Permissions)
                              .OrDefault(new DiscordPermissionSet(DiscordPermission.UseApplicationCommands))
                              .HasPermission((DiscordPermission)50); // USE_EXTERNAL_APPS = 1 << 50; Remora's enums represent a bit shift

        var displayComponents = BookmarkCommands.GetBookmarkComponents(bookmark, canUseDisplayButton: canUseDisplayButton);
        
        return (Result)await interactions.RespondComponentsV2Async(context, components: displayComponents, ephemeral: true, isComponentsV2: true);
    }

    [Button("delete_bookmark")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> DeleteBookmarkAsync(string state)
    {
        _ = context.TryGetUserID(out Snowflake userID);

        Result result = await bookmarks.DeleteBookmarkAsync(state, userID);

        string content = result.IsSuccess
        ? "Bookmark deleted successfully!"
        : "Failed to delete bookmark!";

        IMessageComponent[] components = [new TextDisplayComponent(content)];

        return await interactions.CreateInteractionResponseAsync
        (
            context.Interaction.ID,
            context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.UpdateMessage,
                new(new InteractionMessageCallbackData
                    (
                        Components: components,
                        Flags: MessageFlags.Ephemeral | MessageFlags.IsComponentsV2,
                        AllowedMentions: new AllowedMentions((MentionType[]) [])
                    ))
            )
        );
    }

    [Button("display_bookmark")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> DisplayBookmarkAsync(string state)
    {
        _ = context.TryGetUserID(out Snowflake userID);
        var bookmarkResult = await bookmarks.GetBookmarkAsync(state, userID);

        if (!bookmarkResult.IsDefined(out BookmarkEntity? bookmark))
        {
            return (Result)await interactions.RespondComponentsV2Async(context, "Bookmark not found!!");
        }
        
        await interactions.CreateInteractionResponseAsync
        (
            context.Interaction.ID,
            context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.UpdateMessage,
                new(new InteractionMessageCallbackData
                (
                    AllowedMentions: new AllowedMentions((MentionType[]) []),
                    Components: new(BookmarkCommands.GetBookmarkComponents(bookmark, true)))
                )
            )
        );

        List<IMessageComponent> components = 
        [
            new ContainerComponent
            (
                [
                    new TextDisplayComponent
                    (
                        $"""
                         ## <@{bookmark.UserID}>'s Bookmark
                         -# This bookmark is from <@{bookmark.AuthorID}> in <#{bookmark.ChannelID}>
                         """
                    ),
                    new ActionRowComponent
                    (
                        [
                            new ButtonComponent(ButtonComponentStyle.Danger, "Delete Message", CustomID: CustomIDHelpers.CreateButtonID("delete_public_bookmark_message")),
                            new ButtonComponent(ButtonComponentStyle.Link, "View Message", URL: $"https://discord.com/channels/{bookmark.GuildID}/{bookmark.ChannelID}/{bookmark.MessageID}", IsDisabled: !bookmark.GuildID.HasValue),
                            new ButtonComponent(ButtonComponentStyle.Link, "Add Bookmarker", URL: $"https://canary.discord.com/oauth2/authorize?client_id={context.Interaction.ApplicationID}"),
                        ]
                    ),
                ],
                AccentColour: Color.DarkRed
            ),
            //new SeparatorComponent(IsDivider: true),
        ];

        List<IMessageComponent> secondContainerComponents = [new TextDisplayComponent("## Bookmark Content:")];

        if (!string.IsNullOrEmpty(bookmark.Content))
        {
            secondContainerComponents.Add(new TextDisplayComponent($"> {bookmark.PartialContent}"));
        }

        if (bookmark.Attachments.Length > 0)
        {
            secondContainerComponents.Add(new MediaGalleryComponent(bookmark.Attachments.Select(a => new MediaGalleryItem(new UnfurledMediaItem(a))).ToArray()));
        }

        return (Result)await interactions.RespondComponentsV2Async(context, components: [..components, new ContainerComponent(secondContainerComponents, AccentColour: Color.DeepSkyBlue)], isComponentsV2: true);
    }

    [Button("delete_public_bookmark_message")]
    public async Task<Result> DeletePublicBookmarkMessageAsync()
    {
        return await interactions.DeleteOriginalInteractionResponseAsync(context.Interaction.ApplicationID, context.Interaction.Token);
    }

    [Modal("create_bookmark")]
    [SuppressInteractionResponse(true)]
    public async Task<Result> CreateBookmarkModalAsync(string tags, string state)
    {
        await interactions.CreateInteractionResponseAsync(context.Interaction.ID, context.Interaction.Token, new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage));

        Snowflake? guildID = context.Interaction.GuildID.AsNullable();
        Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

        Result<DataLease<string, IPartialMessage>> leaseResult = await InMemoryDataService<string, IPartialMessage>.Instance.LeaseDataAsync(state);

        if (!leaseResult.IsDefined(out var originalMessageResult))
        {
            return (Result)await interactions.RespondAsync(context, "Failed to retrieve original message!", ephemeral: true);
        }

        await using DataLease<string, IPartialMessage> originalMessage = originalMessageResult;

        string[] tagArray = tags.Split(',');
        Result<BookmarkEntity> bookmark = await bookmarks.CreateBookmarkAsync(userID, guildID, tagArray, originalMessage.Data);

        string content = bookmark.IsSuccess
            ? "Bookmark created!"
            : "Failed to create bookmark!";

        return (Result)await interactions.RespondAsync(context, content, ephemeral: true);
    }

    [Button("forward")]
    public  Task<Result> PaginateForward(string state) => this.PaginateAsync(state);
    
    [Button("backward")]
    public Task<Result> PaginateBackward(string state) => this.PaginateAsync(state);

    private async Task<Result> PaginateAsync(string state)
    {
        _ = context.TryGetUserID(out Snowflake userID);
        
        string[] tagParts = state.Split(':');
        var page = int.Parse(tagParts[0]);
        
        IReadOnlyList<BookmarkEntity> bookmarksResult = await bookmarks.GetBookmarksAsync(userID);
        IReadOnlyList<IMessageComponent> components = BookmarkCommands.CreateComponents(page, bookmarksResult, tagParts[1], BookmarkCommands.MaxBookmarksPerPage);

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            components: new(components),
            allowedMentions: new AllowedMentions((MentionType[]) [])
        );
    }
}