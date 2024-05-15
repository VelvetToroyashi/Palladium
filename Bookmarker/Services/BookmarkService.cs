using System.Text.RegularExpressions;
using Bookmarker.Data;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Bookmarker.Services;

/// <summary>
/// A service to interact with bookmarks.
/// </summary>
/// <param name="contextFactory">A factory to create a new <see cref="BookmarkContext"/>.</param>
public partial class BookmarkService(IDbContextFactory<BookmarkContext> contextFactory)
{
    [GeneratedRegex(@"(?<Link>https:\/\/(?:cdn|media)\.discordapp\.(?:com|net)\/attachments\/\d{17,20}\/\d{17,20}\/\S+\.(?:png|jpe?g|mp4|webm|webp))\b")]
    private static partial Regex GetAttachmentRegex();
    
    private const string BookmarkNotFoundError = "The bookmark you're looking for doesn't exist!";
    
    /// <summary>
    /// Creates a new bookmark.
    /// </summary>
    /// <param name="userId">The ID of the user creating the bookmark.</param>
    /// <param name="guildID">The ID of the guild the bookmark is in if any.</param>
    /// <param name="tags">The tags for the bookmark.</param>
    /// <param name="bookmarkMessage">The message to bookmark.</param>
    /// <returns></returns>
    public async ValueTask<Result<BookmarkEntity>> CreateBookmarkAsync
    (
        Snowflake userId,
        Snowflake? guildID,
        IReadOnlyList<string> tags,
        IPartialMessage bookmarkMessage
    )
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        string partialContent = this.ExtractMessageContent(bookmarkMessage);
        Regex attachmentRegex = GetAttachmentRegex();

        var messageAttachments = bookmarkMessage
                                 .Attachments
                                 .OrDefault([])
                                 .Select(x => attachmentRegex.Match(x.Url).Groups["Link"].Value);

        BookmarkEntity bookmark = new()
        {
            Tags = [..tags],
            UserID = userId,
            GuildID = guildID,
            Attachments = [..messageAttachments],
            MessageID = bookmarkMessage.ID.Value,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorID = bookmarkMessage.Author.Value.ID,
            ChannelID = bookmarkMessage.ChannelID.Value,
            PartialContent = partialContent,
        };

        try
        {
            await context.Bookmarks.AddAsync(bookmark);
            await context.SaveChangesAsync();

            return Results.Successful(bookmark);
        }
        catch(Exception e)
        {
            return e;
        }
    }
    
    /// <summary>
    /// Checks if a user has bookmarked a message.
    /// </summary>
    /// <param name="messageID">The ID of the message.</param>
    /// <param name="userID">The ID of the user attempting to bookmark a message.</param>
    /// <returns>Whether the user has already bookmarked the message.</returns>
    public async Task<bool> HasMessagedBookmarkedAsync(Snowflake messageID, Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        return await context.Bookmarks.AnyAsync(b => b.MessageID == messageID && b.UserID == userID);
    }
    

    private string ExtractMessageContent(IPartialMessage bookmarkMessage)
    {
        var content = bookmarkMessage.Content.OrDefault();

        return string.IsNullOrEmpty(content) ? "[Message does not contain content]" : content.Truncate(45, "[...]");
    }

    /// <summary>
    /// Gets a bookmark by its ID.
    /// </summary>
    /// <param name="id">The ID of the bookmark</param>
    /// <param name="userID">The ID of the user requesting the bookmark</param>
    /// <returns>The bookmark if it exists and the user has access to it.</returns>
    public async ValueTask<Result<BookmarkEntity>> GetBookmarkAsync(string id, Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();        
        BookmarkEntity? bookmark = await context.Bookmarks.FirstOrDefaultAsync(b => b.ID == id);

        return bookmark?.UserID == userID ? 
            Results.Successful(bookmark) : 
            Results.NotFound(BookmarkNotFoundError, bookmark);
    }
    
    /// <summary>
    /// Gets all bookmarks for a user.
    /// </summary>
    /// <param name="userID">The ID of the user to return bookmarks for</param>
    /// <returns>A list of bookmarks for the given user.</returns>
    public async ValueTask<IReadOnlyList<BookmarkEntity>> GetBookmarksAsync(Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        IReadOnlyList<BookmarkEntity> bookmarks = await context.Bookmarks
            .Where(x => x.UserID == userID)
            .ToListAsync();
        
        return bookmarks;
    }
    
    /// <summary>
    /// Deletes a bookmark by its ID.
    /// </summary>
    /// <param name="id">The ID of the bookmark to delete.</param>
    /// <param name="userID">The ID of the user deleting the bookmark.</param>
    /// <returns></returns>
    public async ValueTask<Result> DeleteBookmarkAsync(int id, Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        BookmarkEntity? bookmark = await context.Bookmarks.FindAsync(id);
        
        if (bookmark?.UserID != userID)
        {
            return Results.NotFound(BookmarkNotFoundError);
        }
        
        context.Bookmarks.Remove(bookmark);
        await context.SaveChangesAsync();
        
        return Result.Success;
    }
}