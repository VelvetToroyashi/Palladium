using Microsoft.EntityFrameworkCore;

namespace Bookmarker.Data;

public class BookmarkContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<BookmarkEntity> Bookmarks { get; set; }
}