using System.ComponentModel.DataAnnotations.Schema;

namespace Entity;

public enum Platform
{
    GitHub,
    Twitter,
    LinkedIn,
    YouTube,
    Facebook,
    Twitch,
    DevTo,
    CodeWars,
    FreeCodeCamp,
    GitLab,
    Hashnode,
    StackOverflow,
    FrontendMentor
}

[Table("links")]
public class Link
{
    [Column("id")]
    public int Id { get; set; }

    [Column("platform")]
    public Platform Platform { get; set; }

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("user_profile_id")]
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
}
