using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entity;

[Table("user_profiles")]
public class UserProfile
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("display_email")]
    public string DisplayEmail { get; set; } = string.Empty;

    [Required]
    [Column("profile_slug")]
    public string ProfileSlug { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    [Column("user_id")]
    public int UserId { get; set; }
}
