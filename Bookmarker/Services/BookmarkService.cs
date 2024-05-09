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
public class BookmarkService(IDbContextFactory<BookmarkContext> contextFactory)
{
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
        
        BookmarkEntity bookmark = new()
        {
            Tags = [..tags],
            UserID = userId,
            GuildID = guildID,
            Attachments = [..bookmarkMessage.Attachments.OrDefault([]).Select(x => x.Url)],
            MessageID = bookmarkMessage.ID.Value,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorID = bookmarkMessage.Author.Value.ID,
            ChannelID = bookmarkMessage.ChannelID.Value,
            PartialContent = bookmarkMessage.Content.Value.Truncate(45, "[...]"),
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
    /// Gets a bookmark by its ID.
    /// </summary>
    /// <param name="id">The ID of the bookmark</param>
    /// <param name="userID">The ID of the user requesting the bookmark</param>
    /// <returns>The bookmark if it exists and the user has access to it.</returns>
    public async ValueTask<Result<BookmarkEntity>> GetBookmarkAsync(int id, Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();        
        BookmarkEntity? bookmark = await context.Bookmarks.FindAsync(id);

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