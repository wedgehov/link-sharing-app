using System.ComponentModel.DataAnnotations.Schema;

namespace Entity;

[Table("user_profiles")]
public class UserProfile
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }
    
    [Column("display_email")]
    public string? DisplayEmail { get; set; }

    [Column("profile_slug")]
    public string ProfileSlug { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Link> Links { get; set; } = new List<Link>();
}
