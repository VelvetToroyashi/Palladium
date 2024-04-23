using System.ComponentModel.DataAnnotations;
using Remora.Rest.Core;

namespace Bookmarker.Data;

/// <summary>
/// Represents a bookmark entity.
/// </summary>
public class BookmarkEntity
{
    public int Id { get; set; }
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