using Bookmarker.Data;
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
        IMessage bookmarkMessage
    )
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        BookmarkEntity bookmark = new()
        {
            Tags = [..tags],
            UserID = userId,
            GuildID = guildID,
            Attachments = [..bookmarkMessage.Attachments.Select(x => x.Url)],
            MessageID = bookmarkMessage.ID,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorID = bookmarkMessage.Author.ID,
            ChannelID = bookmarkMessage.ChannelID,
            PartialContent = bookmarkMessage.Content,
        };
        
        await context.Bookmarks.AddAsync(bookmark);
        await context.SaveChangesAsync();
        
        return Results.Successful(bookmark);
    }
    
    public async ValueTask<Result<BookmarkEntity>> GetBookmarkAsync(int id, Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();        
        BookmarkEntity? bookmark = await context.Bookmarks.FindAsync(id);

        return bookmark?.UserID == userID ? 
            Results.Successful(bookmark) : 
            Results.NotFound(BookmarkNotFoundError, bookmark);
    }
    
    public async ValueTask<IReadOnlyList<BookmarkEntity>> GetBookmarksAsync(Snowflake userID)
    {
        await using BookmarkContext context = await contextFactory.CreateDbContextAsync();
        
        IReadOnlyList<BookmarkEntity> bookmarks = await context.Bookmarks
            .Where(x => x.UserID == userID)
            .ToListAsync();
        
        return bookmarks;
    }
    
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