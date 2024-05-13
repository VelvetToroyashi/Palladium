using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Remora.Rest.Core;

namespace Bookmarker.Data;

/// <summary>
/// Represents a bookmark entity.
/// </summary>
public class BookmarkEntity
{
    public string ID { get; set; }

    public Snowflake UserID { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public string[] Tags { get; set; }
    
    public string[] Attachments { get; set; }
    
    [StringLength(50)]
    public string? PartialContent { get; set; }
    
    public Snowflake AuthorID { get; set; }
    
    public Snowflake ChannelID { get; set; }
    public Snowflake MessageID { get; set; }
    public Snowflake? GuildID { get; set; }
}

file class BookmarkEntityConfiguration : IEntityTypeConfiguration<BookmarkEntity>
{
    public void Configure(EntityTypeBuilder<BookmarkEntity> builder)
    {
        builder.HasKey(bookmark => new { bookmark.MessageID, bookmark.UserID });

        builder.Property(bookmark => bookmark.ID)
               .IsRequired()
               .ValueGeneratedOnAdd()
               .HasValueGenerator(typeof(BookmarkIdGenerator));
    }
}

file class BookmarkIdGenerator : ValueGenerator
{
    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <inheritdoc />
    protected override object NextValue(EntityEntry entry)
        => RandomNumberGenerator.GetHexString(5, true);
}



