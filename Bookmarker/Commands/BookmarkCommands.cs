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
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;
using RemoraHTTPInteractions.Services;


namespace Bookmarker.Commands;

/// <summary>
/// Commands for managing bookmarks.
/// </summary>
[PublicAPI]
public class BookmarkCommands
(
     IInteractionContext context,
     IDiscordRestInteractionAPI interactions,
     BookmarkService bookmarks
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
     
     [Command("Bookmark This!")]
     [CommandType(ApplicationCommandType.Message)]
     [AllowedContexts(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
     [DiscordInstallContext(ApplicationIntegrationType.UserInstallable)]
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
     [AllowedContexts(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
     [DiscordInstallContext(ApplicationIntegrationType.UserInstallable)]
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
     
     [Command("get_bookmarks")]
     [CommandType(ApplicationCommandType.ChatInput)]
     [AllowedContexts(InteractionContextType.Guild, InteractionContextType.PrivateChannel, InteractionContextType.BotDM)]
     [DiscordInstallContext(ApplicationIntegrationType.UserInstallable)]
     public async Task<Result> GetBookmarksAsync(string? tag = null)
     {
          var contextCopy = context;
          Snowflake userID = contextCopy.Interaction.Member.Map(m => m.User.Value).OrDefault(() => context.Interaction.User.Value).ID;
          
          IReadOnlyList<BookmarkEntity> userBookmarks = await bookmarks.GetBookmarksAsync(userID);
          
          if (userBookmarks.Count is 0)
          {
               return (Result)await interactions.RespondAsync(context, NoBookmarksMessage, ephemeral: true);
          }

          int pageCount = userBookmarks.Count / 25 + 1;

          StringBuilder sb = new();
          ISelectOption[] options = new ISelectOption[Math.Min(userBookmarks.Count, 25)];

          IReadOnlyList<IMessageComponent> navButtons = new[]
          {
               new ButtonComponent(ButtonComponentStyle.Primary,   Label: "\u23ea", CustomID: CustomIDHelpers.CreateButtonID("back"),          IsDisabled: pageCount is 1),
               new ButtonComponent(ButtonComponentStyle.Secondary, Label: "\u200B", CustomID: CustomIDHelpers.CreateButtonID("placeholder_1"), IsDisabled: true),
               new ButtonComponent(ButtonComponentStyle.Secondary, Label: "\u200B", CustomID: CustomIDHelpers.CreateButtonID("placeholder_2"), IsDisabled: true),
               new ButtonComponent(ButtonComponentStyle.Primary,   Label: "\u23e9", CustomID: CustomIDHelpers.CreateButtonID("forward"),       IsDisabled: pageCount is 1),
          };

          for (var i = 0; i < userBookmarks.Take(25).ToArray().Length; i++)
          {
               BookmarkEntity bookmark = userBookmarks.Take(25).ToArray()[i];

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
          
          StringSelectComponent dropdown = new(CustomIDHelpers.CreateSelectMenuID("bookmarks"), options, "Select a bookmark", MaxValues: 1);

          IEmbed embed = new Embed
          {
               Title = tag is null ? "Your bookmarks" : $"Your bookmarks with the tag {tag}",
               Description = sb.ToString(),
               Colour = tag is null ? Color.PaleGreen : Color.LightBlue,
               Footer = pageCount > 1 ? new EmbedFooter($"Page 1 of {pageCount}") : default(Optional<IEmbedFooter>),
          };
          
          IReadOnlyList<IReadOnlyList<IMessageComponent>> sentComponents = pageCount is 1 ? [[dropdown]] : [[dropdown], navButtons];
          return (Result)await interactions.RespondAsync(context, embeds: [embed],  components: sentComponents,  ephemeral: true);
     }
}

public class BookmarkComponentHandler(IDiscordRestInteractionAPI interactions, IInteractionContext context, BookmarkService bookmarks) : InteractionGroup
{
     [SelectMenu("bookmarks")]
     public async Task<Result> ViewBookmarkAsync(IReadOnlyList<string> values)
     {
          string selected = values[0];
          _ = context.TryGetUserID(out Snowflake userID);
          
          Result<BookmarkEntity> bookmark = await bookmarks.GetBookmarkAsync(selected, userID);
          
          if (!bookmark.IsSuccess)
          {
               return (Result)await interactions.RespondAsync(context, $"Failed to retrieve bookmark! \n {bookmark.Error}", ephemeral: true);
          }
          
          int embedCount = bookmark.Entity.Attachments.Length > 1 ? bookmark.Entity.Attachments.Length : 1;
          Embed[] embeds = new Embed[embedCount];
          
          // Create the first embed outside the loop
          Embed firstEmbed = new()
          {
              Title = $"Bookmark {bookmark.Entity.ID}",
              Description = $"""
                             Message posted by <@{bookmark.Entity.AuthorID}> in <#{bookmark.Entity.ChannelID}> <t:{bookmark.Entity.CreatedAt.ToUnixTimeSeconds()}:R>:
                             
                             {bookmark.Entity.PartialContent}
                             """,
              Colour = Color.LightBlue,
          };

          if (bookmark.Entity.Attachments.Length >= 1)
          {
              firstEmbed = firstEmbed with { Image = new EmbedImage(bookmark.Entity.Attachments[0]) };
          }

          embeds[0] = firstEmbed;

          // Create the remaining embeds within the loop
          for (var i = 1; i < embedCount; i++)
          {
              embeds[i] = new Embed
              {
                  Image = new EmbedImage(bookmark.Entity.Attachments[i]),
                  Colour = Color.LightBlue,
              };
          }
          
          IMessageComponent[] components =
          [
               new ButtonComponent(ButtonComponentStyle.Link, "Jump to message", URL: $"https://discord.com/channels/{bookmark.Entity.GuildID?.ToString() ?? "@me"}/{bookmark.Entity.ChannelID}/{bookmark.Entity.MessageID}"),
               new ButtonComponent(ButtonComponentStyle.Danger, "Delete bookmark", CustomID: CustomIDHelpers.CreateButtonIDWithState("DeleteBookmark", bookmark.Entity.ID), IsDisabled: true),
          ];

          Console.WriteLine(embeds.Length);

          return (Result)await interactions.RespondAsync(context, embeds: embeds, components: [components], ephemeral: true);
     }
     
     [Button("DeleteBookmark")]
     public async Task<Result> DeleteBookmarkAsync(string state)
     {
         _ = context.TryGetUserID(out Snowflake userID);

         Result result = await bookmarks.DeleteBookmarkAsync(int.Parse(state), userID);

         string content = result.IsSuccess
             ? "Bookmark deleted successfully!"
             : "Failed to delete bookmark!";

         return (Result)await interactions.RespondAsync(context, content, ephemeral: true);
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
     
}