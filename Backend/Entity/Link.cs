using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entity;

public enum Platform
{
    GitHub = 0,
    Twitter = 1,
    LinkedIn = 2,
    YouTube = 3,
    Facebook = 4,
    Twitch = 5,
    DevTo = 6,
    CodeWars = 7,
    FreeCodeCamp = 8,
    GitLab = 9,
    Hashnode = 10,
    StackOverflow = 11,
    FrontendMentor = 12
}

[Table("links")]
public class Link
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("platform")]
    public Platform Platform { get; set; }

    [Required]
    [Column("url")]
    public string Url { get; set; } = null!;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
}
