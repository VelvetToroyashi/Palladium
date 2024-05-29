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
using Remora.Rest.Core;
using Remora.Results;
using RemoraHTTPInteractions.Services;


namespace Bookmarker.Commands;

/// <summary>
/// Commands for managing bookmarks.
/// </summary>
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
                                          **{0}.** <@{1}> in <#{2}>:
                                          > {3}
                                          This bookmark has {4} attachment(s).

                                          """;

     public const int MaxBookmarksPerPage = 12;

     internal static readonly IMessageComponent[] InitialNavButtons =
     [
          new ButtonComponent(ButtonComponentStyle.Primary,   Label: "\u23ea", CustomID: "placeholder_0",                                         IsDisabled: true),
          new ButtonComponent(ButtonComponentStyle.Secondary, Label: "\u200B", CustomID: "placeholder_1",                                         IsDisabled: true),
          new ButtonComponent(ButtonComponentStyle.Secondary, Label: "\u200B", CustomID: "placeholder_2",                                         IsDisabled: true),
          new ButtonComponent(ButtonComponentStyle.Primary,   Label: "\u23e9", CustomID: CustomIDHelpers.CreateButtonIDWithState("forward", "2"), IsDisabled: false),
     ];

     [Command("Bookmark This!")]
     [CommandType(ApplicationCommandType.Message)]
     public async Task<Result> CreateQuickBookmarkAsync(IPartialMessage message)
     {
          Snowflake? guildID = context.Interaction.GuildID.AsNullable();
          Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

          Result<BookmarkEntity> bookmark = await bookmarks.CreateBookmarkAsync(userID, guildID, [], message);

          string content = bookmark.IsSuccess
              ? "Bookmark created!"
              : "Failed to create bookmark!";

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
          InMemoryDataStore<string, IPartialMessage>.Instance.TryAddValue(state, message);

          InteractionModalCallbackData callbackData = new
          (
               CustomIDHelpers.CreateModalIDWithState("create_bookmark", state),
               "Create a bookmark",
               new IMessageComponent[]
               {
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
                    )
               }
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

     [Command("bookmark_list")]
     [Description("Get a list of your bookmarks.")]
     public async Task<Result> GetBookmarksAsync([Description("Filter by bookmarks with this tag")] string? tag = null)
     {
          Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

          IReadOnlyList<BookmarkEntity> userBookmarks = await bookmarks.GetBookmarksAsync(userID);

          if (userBookmarks.Count is 0)
          {
               return (Result)await interactions.RespondAsync(context, NoBookmarksMessage, ephemeral: true);
          }

          CreateEmbedAndSelectComponent(1, userBookmarks, tag, out IEmbed embed, out ISelectMenuComponent dropdown);

          IReadOnlyList<IReadOnlyList<IMessageComponent>> sentComponents = [[dropdown]];

          if (userBookmarks.Count > MaxBookmarksPerPage)
          {
               sentComponents = [[dropdown], InitialNavButtons];
          }

          return (Result)await interactions.RespondAsync(context, embeds: [embed], components: sentComponents, ephemeral: true);
     }

     [Command("invite")]
     [Description("Get the invite link for the bot.")]
     public async Task<Result> GetInviteLinkAsync()
          => (Result)await interactions.RespondAsync(context, $"https://discord.com/api/oauth2/authorize?client_id={context.Interaction.ApplicationID}", ephemeral: true);

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

public class BookmarkComponentHandler(IDiscordRestInteractionAPI interactions, IInteractionContext context, BookmarkService bookmarks) : InteractionGroup
{
     [SelectMenu("show_bookmark")]
     public async Task<Result> ViewBookmarkAsync(IReadOnlyList<string> values)
     {
          string selected = values[0];
          _ = context.TryGetUserID(out Snowflake userID);

          Result<BookmarkEntity> bookmarkResult = await bookmarks.GetBookmarkAsync(selected, userID);

          if (!bookmarkResult.IsDefined(out BookmarkEntity? bookmark))
          {
               return (Result)await interactions.RespondAsync(context, $"Failed to retrieve bookmark! \n {bookmarkResult.Error}", ephemeral: true);
          }

          int embedCount = bookmark.Attachments.Length > 1 ? bookmark.Attachments.Length : 1;
          Embed[] embeds = new Embed[embedCount];

          string bookmarkTagString = bookmark.Tags.Length > 0 ? string.Join(", ", bookmark.Tags) : "None";

          // Create the first embed outside the loop
          Embed firstEmbed = new()
          {
              Title = $"Bookmark {bookmarkResult.Entity.ID}",
              Fields = (IEmbedField[])
              [
                new EmbedField("Tags", bookmarkTagString, true),
                new EmbedField("Attachments", bookmark.Attachments.Length.ToString(), true),
                new EmbedField("Bookmarked", $"<t:{bookmark.CreatedAt.ToUnixTimeSeconds()}:R>", true),
                new EmbedField("Author", $"<@{bookmark.AuthorID}>", true),
                new EmbedField("Channel", $"<#{bookmark.ChannelID}>", true),
                new EmbedField("Content", bookmark.PartialContent!),
              ],
              Colour = Color.LightBlue,
          };

          if (bookmarkResult.Entity.Attachments.Length >= 1)
          {
              firstEmbed = firstEmbed with { Image = new EmbedImage(bookmarkResult.Entity.Attachments[0]) };
          }

          embeds[0] = firstEmbed;

          // Create the remaining embeds within the loop
          for (var i = 1; i < embedCount; i++)
          {
              embeds[i] = new Embed
              {
                  Image = new EmbedImage(bookmarkResult.Entity.Attachments[i]),
                  Colour = Color.LightBlue,
              };
          }

          IMessageComponent[] components =
          [
               new ButtonComponent(ButtonComponentStyle.Link, "Jump to message", URL: $"https://discord.com/channels/{bookmarkResult.Entity.GuildID?.ToString() ?? "@me"}/{bookmarkResult.Entity.ChannelID}/{bookmarkResult.Entity.MessageID}"),
               new ButtonComponent(ButtonComponentStyle.Danger, "Delete bookmark", CustomID: CustomIDHelpers.CreateButtonIDWithState("delete_bookmark", bookmarkResult.Entity.ID)),
          ];

          return (Result)await interactions.RespondAsync(context, embeds: embeds, components: [components], ephemeral: true);
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

        return await interactions.CreateInteractionResponseAsync
        (
             context.Interaction.ID,
             context.Interaction.Token,
             new InteractionResponse
             (
                  InteractionCallbackType.UpdateMessage,
                  new(new InteractionMessageCallbackData
                  (
                       Content: content,
                       Embeds: (IEmbed[])[],
                       Components: (IMessageComponent[])[],
                       Flags: MessageFlags.Ephemeral
                  ))
             )
        );
     }

     [Modal("create_bookmark")]
     [SuppressInteractionResponse(true)]
     public async Task<Result> CreateBookmarkModalAsync(string tags, string state)
     {
          await interactions.CreateInteractionResponseAsync(context.Interaction.ID, context.Interaction.Token, new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage));

          Snowflake? guildID = context.Interaction.GuildID.AsNullable();
          Snowflake userID = context.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;

          Result<DataLease<string, IPartialMessage>> leaseResult = await InMemoryDataStore<string, IPartialMessage>.Instance.TryGetLeaseAsync(state);

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
     public async Task<Result> PaginateForward(int state)
     {
          _ = context.TryGetUserID(out Snowflake userID);

          await interactions.CreateInteractionResponseAsync
          (
               context.Interaction.ID,
               context.Interaction.Token,
               new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage)
          );

          IReadOnlyList<BookmarkEntity> bookmarksResult = await bookmarks.GetBookmarksAsync(userID);

          BookmarkCommands.CreateEmbedAndSelectComponent(state, bookmarksResult, null, out IEmbed embed, out ISelectMenuComponent selectMenu);

          bool hasFuturePages = bookmarksResult.Count / BookmarkCommands.MaxBookmarksPerPage + 1 > state + 1;

          IMessageComponent[] updatedNavButtons =
          [
               (BookmarkCommands.InitialNavButtons[0] as ButtonComponent)! with { IsDisabled = false, CustomID = CustomIDHelpers.CreateButtonIDWithState("backward", $"{state - 1}")},
               ..BookmarkCommands.InitialNavButtons[1..^1],
               (BookmarkCommands.InitialNavButtons[3] as ButtonComponent)! with { IsDisabled = !hasFuturePages, CustomID = CustomIDHelpers.CreateButtonIDWithState("forward", $"{state + 1}")},
          ];

          IMessageComponent[] wrappedComponents = [new ActionRowComponent([selectMenu]), new ActionRowComponent(updatedNavButtons)];

          return (Result)await interactions.EditOriginalInteractionResponseAsync
          (
               context.Interaction.ApplicationID,
               context.Interaction.Token,
               embeds: (IEmbed[])[embed],
               components: wrappedComponents
          );
     }

     [Button("backward")]
     public async Task<Result> PaginateBackward(int state)
     {
          _ = context.TryGetUserID(out Snowflake userID);

          await interactions.CreateInteractionResponseAsync
          (
               context.Interaction.ID,
               context.Interaction.Token,
               new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage)
          );

          IReadOnlyList<BookmarkEntity> bookmarksResult = await bookmarks.GetBookmarksAsync(userID);

          BookmarkCommands.CreateEmbedAndSelectComponent(state, bookmarksResult, null, out IEmbed embed, out ISelectMenuComponent selectMenu);

          // This is the backward button, so we know there's going to be a future page.
          IMessageComponent[] updatedNavButtons =
          [
               (BookmarkCommands.InitialNavButtons[0] as ButtonComponent)! with { IsDisabled = state is 1, CustomID = CustomIDHelpers.CreateButtonIDWithState("backward", $"{state - 1}")},
               ..BookmarkCommands.InitialNavButtons[1..^1],
               (BookmarkCommands.InitialNavButtons[3] as ButtonComponent)! with { IsDisabled = false, CustomID = CustomIDHelpers.CreateButtonIDWithState("forward", $"{state + 1}")},
          ];

          IMessageComponent[] wrappedComponents = [new ActionRowComponent([selectMenu]), new ActionRowComponent(updatedNavButtons)];

          return (Result)await interactions.EditOriginalInteractionResponseAsync
          (
               context.Interaction.ApplicationID,
               context.Interaction.Token,
               embeds: (IEmbed[])[embed],
               components: wrappedComponents
          );
     }

}
